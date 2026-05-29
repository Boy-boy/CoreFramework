using K4os.Compression.LZ4;
using MemoryPack;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;

namespace Core.Redis
{
    public static class RedisExtensions
    {
        // 压缩和分片的长、短后缀命名，缩短 Key 长度，节约 Redis 内存，同时保持生产环境可读性
        private const string ChunkSuffix = ":chk:";
        private const string MetaSuffix = ":meta"; // 自描述头（version+算法+分片数+长度），取代旧的纯计数 :cnt
        private const int ChunkSize = 1 * 1024 * 1024; // 1MB 物理分片大小

        // ---- 自描述头格式（共 17 字节，小端）----
        // [0]=Magic0 'L' [1]=Magic1 'O' [2]=Version
        // [3]=CompressionAlgorithm [4]=SerializationAlgorithm
        // [5..8]=chunkCount(int32) [9..12]=originalLength(int32) [13..16]=compressedLength(int32)
        private const byte HeaderMagic0 = 0x4C; // 'L'
        private const byte HeaderMagic1 = 0x4F; // 'O'
        private const byte HeaderVersion = 1;
        private const int HeaderSize = 17;

        public static Task SetLargeObjectAsync<T>(this IRedisCache redis, string key, T value,
            TimeSpan? expiry = null, int db = -1, CompressionAlgorithm algorithm = CompressionAlgorithm.Lz4,
            SerializationAlgorithm serializationAlgorithm = SerializationAlgorithm.MessagePack,
            CancellationToken cancellationToken = default)
        {
            var impl = RequireStackExchangeRedis(redis);
            return impl.SetLargeObjectAsync(key, value, expiry, db, algorithm, serializationAlgorithm, cancellationToken);
        }

        public static Task<T?> GetLargeObjectAsync<T>(this IRedisCache redis, string key,
            int db = -1, CompressionAlgorithm algorithm = CompressionAlgorithm.Lz4,
            SerializationAlgorithm serializationAlgorithm = SerializationAlgorithm.MessagePack,
            CancellationToken cancellationToken = default)
        {
            var impl = RequireStackExchangeRedis(redis);
            return impl.GetLargeObjectAsync<T>(key, db, algorithm, serializationAlgorithm, cancellationToken);
        }

        private static StackExchangeRedis RequireStackExchangeRedis(IRedisCache redis)
        {
            if (redis == null) throw new ArgumentNullException(nameof(redis));
            return redis as StackExchangeRedis
                ?? throw new NotSupportedException($"{nameof(SetLargeObjectAsync)}/{nameof(GetLargeObjectAsync)} 仅支持 {nameof(StackExchangeRedis)} 实现。");
        }

        /// <summary>
        /// 全流程低 GC、高性能零拷贝大对象分片写入。
        /// 所有分片键与元数据键统一加 hash tag，保证 Redis Cluster 下落在同一 slot，
        /// 事务（MULTI/EXEC）与多键删除不会触发 CROSSSLOT。
        /// 注意：MessagePack 序列化（<see cref="SerializationHelper"/>）已内置 Lz4BlockArray 压缩，
        /// 若再叠加外层 <see cref="CompressionAlgorithm.Lz4"/> 属于二次压缩、收益为负；
        /// 此组合建议外层传 <see cref="CompressionAlgorithm.None"/>。
        /// </summary>
        public static async Task SetLargeObjectAsync<T>(this StackExchangeRedis redis, string key, T value,
            TimeSpan? expiry = null, int db = -1, CompressionAlgorithm algorithm = CompressionAlgorithm.Lz4,
            SerializationAlgorithm serializationAlgorithm = SerializationAlgorithm.MessagePack,
            CancellationToken cancellationToken = default)
        {
            if (redis == null) throw new ArgumentNullException(nameof(redis));
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();

            var prefixedKey = (string)redis.Options.GetPrefixedKey(key);
            var tag = BuildHashTag(prefixedKey);
            var database = redis.GetDatabase(db);

            byte[]? compressedRentBuffer = null;

            try
            {
                // 1. 序列化（MemoryPack 直接写入 IBufferWriter；MessagePack 走 helper）
                var serializedMemory = Serialize(value, serializationAlgorithm);
                var originalLength = serializedMemory.Length;

                // 2. 压缩（None 为 0 拷贝直通；Lz4 就地 Span 压缩并池化）
                var (compressedData, compressRent) = Compress(serializedMemory, algorithm);
                compressedRentBuffer = compressRent;
                var compressedLength = compressedData.Length;

                cancellationToken.ThrowIfCancellationRequested();

                // 读取旧的自描述头拿到旧分片数：用于在同一事务里删除"多出来的"旧尾片，
                // 而非提前物理清空。提前清空会制造缓存穿透窗口，且写入失败会丢掉本可保留的旧值。
                var oldChunkCount = await ReadChunkCountAsync(database, tag).ConfigureAwait(false);

                // 3. 计算分片（ReadOnlyMemory.Slice 逻辑切片，0 拷贝）
                var chunkCount = (compressedLength + ChunkSize - 1) / ChunkSize;

                var tx = database.CreateTransaction();
                var txTasks = new List<Task>(chunkCount + 1 + Math.Max(0, oldChunkCount - chunkCount));

                for (var i = 0; i < chunkCount; i++)
                {
                    var offset = i * ChunkSize;
                    var length = Math.Min(ChunkSize, compressedLength - offset);
                    var chunkSlice = compressedData.Slice(offset, length);

                    // StackExchange.Redis 原生支持直接将 ReadOnlyMemory<byte> 作为 Value 写入
                    txTasks.Add(tx.StringSetAsync($"{tag}{ChunkSuffix}{i}", chunkSlice, expiry));
                }

                // 删除旧值多出来的尾部分片（旧 M 个、新 N 个且 M > N 时）。
                // 索引 0..N-1 已被上面的写入直接覆盖，无需先删；只有 N..M-1 是残余，
                // 在事务内一并清除，避免无 TTL 时永久泄漏。整个替换原子完成，读侧永远看到一致的 (meta, chunks)。
                for (var i = chunkCount; i < oldChunkCount; i++)
                {
                    txTasks.Add(tx.KeyDeleteAsync($"{tag}{ChunkSuffix}{i}"));
                }

                // 4. 写入自描述头（供读取端精确解压、自识别算法、做完整性校验）
                var header = BuildHeader(algorithm, serializationAlgorithm, chunkCount, originalLength, compressedLength);
                txTasks.Add(tx.StringSetAsync($"{tag}{MetaSuffix}", header, expiry));

                // 5. 原子提交事务：旧值在此刻被原子替换，期间并发 Get 始终读到旧的完整数据，无穿透窗口
                var committed = await tx.ExecuteAsync().ConfigureAwait(false);
                if (!committed)
                    throw new InvalidOperationException($"Redis transaction for large object '{key}' was aborted.");

                // 等所有子命令完成后再让 finally 归还压缩缓冲：此时载荷字节早已写入 socket，归还安全
                await Task.WhenAll(txTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                redis.Logger.LogError(ex, "Failed to set large object for key: {Key}", key);

                // 事务是原子的：失败时要么旧值完整保留、要么新值已完整写入，不存在需要清理的半成品分片，
                // 因此不再在失败路径做物理清空（否则反而会误删本可保留的旧值）。
                if (ex is OperationCanceledException) throw;
                throw new InvalidOperationException($"Failed to set large object for key: {key}", ex);
            }
            finally
            {
                // 将租借的内存安全归还到全局对象池
                if (compressedRentBuffer != null) ArrayPool<byte>.Shared.Return(compressedRentBuffer);
            }
        }

        /// <summary>
        /// 高并发优化版：并发管道拉取分片 + 快速合并解压。
        /// 算法与原始长度从自描述头读取（不依赖调用方传参一致），
        /// 元数据缺失/格式不符/分片缺失（如短 TTL 下部分过期）一律按缓存未命中返回 default。
        /// </summary>
        public static async Task<T?> GetLargeObjectAsync<T>(this StackExchangeRedis redis, string key,
            int db = -1, CompressionAlgorithm algorithm = CompressionAlgorithm.Lz4,
            SerializationAlgorithm serializationAlgorithm = SerializationAlgorithm.MessagePack,
            CancellationToken cancellationToken = default)
        {
            if (redis == null) throw new ArgumentNullException(nameof(redis));
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();

            var prefixedKey = (string)redis.Options.GetPrefixedKey(key);
            var tag = BuildHashTag(prefixedKey);
            var database = redis.GetDatabase(db);

            try
            {
                // 读取自描述头
                var headerValue = await database.StringGetAsync($"{tag}{MetaSuffix}").ConfigureAwait(false);
                if (headerValue.IsNullOrEmpty) return default;

                if (!TryParseHeader((byte[])headerValue!, out var meta))
                {
                    // 旧格式或损坏的元数据 —— 按缓存未命中处理，不抛异常打断业务
                    redis.Logger.LogWarning("Large object meta for key {Key} is missing or in an unexpected format; treated as cache miss.", key);
                    return default;
                }

                var chunkCount = meta.ChunkCount;
                if (chunkCount <= 0) return default;

                cancellationToken.ThrowIfCancellationRequested();

                // 租借分片临时指针数组（避免引发高频小对象 GC）
                var chunks = ArrayPool<byte[]>.Shared.Rent(chunkCount);
                try
                {
                    // 不用 CreateBatch：所有分片同 slot，直接发起异步命令由驱动自动 pipeline 并发，
                    // 避免显式 Batch 独占发送队列、阻塞同一连接上的其它轻量命令。
                    var tasks = new Task<RedisValue>[chunkCount];
                    for (var i = 0; i < chunkCount; i++)
                    {
                        tasks[i] = database.StringGetAsync($"{tag}{ChunkSuffix}{i}");
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    var totalLength = 0;
                    for (var i = 0; i < chunkCount; i++)
                    {
                        var res = tasks[i].Result;
                        if (res.IsNullOrEmpty)
                        {
                            // 分片缺失（典型为短 TTL 下元数据与分片之间的过期竞态）—— 视为缓存未命中
                            redis.Logger.LogWarning("Large object key {Key} is missing chunk {Index}; treated as cache miss.", key, i);
                            return default;
                        }

                        var bytes = (byte[])res!;
                        chunks[i] = bytes;
                        totalLength += bytes.Length;
                    }

                    // 完整性校验：合并后的压缩字节数应与头里记录的一致
                    if (totalLength != meta.CompressedLength)
                    {
                        redis.Logger.LogWarning(
                            "Large object key {Key} integrity mismatch: expected {Expected} compressed bytes, got {Actual}; treated as cache miss.",
                            key, meta.CompressedLength, totalLength);
                        return default;
                    }

                    // 租借一个连续的、能容纳全部压缩数据的物理大数组，杜绝 LOH（大对象堆）内存震荡
                    var combinedBuffer = ArrayPool<byte>.Shared.Rent(totalLength);
                    byte[]? decompressedBuffer = null;
                    try
                    {
                        // 零分配高速内存拷贝合并
                        CombineChunksDirect(chunks, chunkCount, combinedBuffer);

                        // 解压：原始长度来自头，LZ4 据此精确租借，杜绝旧版 "压缩长度 ×10" 启发式的越界失败与整数溢出
                        var (decompressedData, decompressedLength, decompRent) =
                            Decompress(combinedBuffer, totalLength, meta.OriginalLength, meta.Compression);
                        decompressedBuffer = decompRent;

                        // 反序列化还原成泛型对象（按 length 精确收束，避免读到池化数组尾部脏数据）
                        return Deserialize<T>(decompressedData, decompressedLength, meta.Serialization);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(combinedBuffer);
                        if (decompressedBuffer != null) ArrayPool<byte>.Shared.Return(decompressedBuffer);
                    }
                }
                finally
                {
                    // 悬挂指针清理：归还前清空我们在 ArrayPool 中占用的槽位
                    for (var i = 0; i < chunkCount; i++) chunks[i] = null!;
                    ArrayPool<byte[]>.Shared.Return(chunks);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                redis.Logger.LogError(ex, "Failed to get large object for key: {Key}", key);
                throw new InvalidOperationException($"Failed to get large object for key: {key}", ex);
            }
        }

        // 读取旧的自描述头中的分片数；元数据缺失或格式不符返回 0（视为无旧分片需清理）
        private static async Task<int> ReadChunkCountAsync(IDatabase database, string tag)
        {
            var headerValue = await database.StringGetAsync($"{tag}{MetaSuffix}").ConfigureAwait(false);
            if (headerValue.IsNullOrEmpty) return 0;
            return TryParseHeader((byte[])headerValue!, out var meta) ? meta.ChunkCount : 0;
        }

        private static void CombineChunksDirect(byte[][] chunks, int count, byte[] destination)
        {
            var offset = 0;
            for (var i = 0; i < count; i++)
            {
                var chunk = chunks[i];
                Buffer.BlockCopy(chunk, 0, destination, offset, chunk.Length);
                offset += chunk.Length;
            }
        }

        // 同一逻辑对象的所有分片/元数据键共享 hash tag，强制落到同一 Cluster slot
        private static string BuildHashTag(string prefixedKey) => "{" + prefixedKey + "}";

        #region 自描述头编解码

        private static byte[] BuildHeader(CompressionAlgorithm compression, SerializationAlgorithm serialization,
            int chunkCount, int originalLength, int compressedLength)
        {
            var header = new byte[HeaderSize];
            header[0] = HeaderMagic0;
            header[1] = HeaderMagic1;
            header[2] = HeaderVersion;
            header[3] = (byte)compression;
            header[4] = (byte)serialization;
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(5), chunkCount);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(9), originalLength);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(13), compressedLength);
            return header;
        }

        private static bool TryParseHeader(byte[] raw,
            out (CompressionAlgorithm Compression, SerializationAlgorithm Serialization, int ChunkCount, int OriginalLength, int CompressedLength) meta)
        {
            meta = default;
            if (raw == null || raw.Length < HeaderSize) return false;
            if (raw[0] != HeaderMagic0 || raw[1] != HeaderMagic1 || raw[2] != HeaderVersion) return false;

            var compression = (CompressionAlgorithm)raw[3];
            var serialization = (SerializationAlgorithm)raw[4];
            if (!Enum.IsDefined(compression) || !Enum.IsDefined(serialization)) return false;

            var chunkCount = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(5));
            var originalLength = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(9));
            var compressedLength = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(13));
            if (chunkCount < 0 || originalLength < 0 || compressedLength < 0) return false;

            meta = (compression, serialization, chunkCount, originalLength, compressedLength);
            return true;
        }

        #endregion

        #region 💡 低分配核心内存管理流水线

        private static ReadOnlyMemory<byte> Serialize<T>(T value, SerializationAlgorithm algorithm)
        {
            if (algorithm == SerializationAlgorithm.MemoryPack)
            {
                var writer = new ArrayBufferWriter<byte>(ChunkSize);
                MemoryPackSerializer.Serialize(writer, value);
                // 直接提取已写入的有效内存段视图，0 内存浪费
                return writer.WrittenMemory;
            }

            // MessagePack：helper 对 null 返回 null
            var bytes = SerializationHelper.Serialize(value);
            return bytes ?? ReadOnlyMemory<byte>.Empty;
        }

        private static T Deserialize<T>(byte[] data, int length, SerializationAlgorithm algorithm)
        {
            // 通过 length 边界收束，阻止反序列化框架读取到 ArrayPool 数组末尾残留的垃圾脏数据
            return algorithm switch
            {
                SerializationAlgorithm.MemoryPack => MemoryPackSerializer.Deserialize<T>(new ReadOnlySpan<byte>(data, 0, length))!,
                SerializationAlgorithm.MessagePack => SerializationHelper.Deserialize<T>(new ReadOnlyMemory<byte>(data, 0, length)),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
            };
        }

        private static (ReadOnlyMemory<byte> Data, byte[]? RentArray) Compress(ReadOnlyMemory<byte> source, CompressionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case CompressionAlgorithm.None:
                    // 0 拷贝直通；不分配、不池化
                    return (source, null);

                case CompressionAlgorithm.Lz4:
                    {
                        if (source.Length == 0) return (ReadOnlyMemory<byte>.Empty, null);

                        var maxLen = LZ4Codec.MaximumOutputSize(source.Length);
                        var rent = ArrayPool<byte>.Shared.Rent(maxLen);

                        // 高性能通过 Span 进行就地极速压缩
                        var compressedLen = LZ4Codec.Encode(source.Span, rent.AsSpan(0, rent.Length), LZ4Level.L00_FAST);
                        if (compressedLen < 0)
                        {
                            ArrayPool<byte>.Shared.Return(rent);
                            throw new InvalidOperationException("LZ4 compression failed due to insufficient output buffer.");
                        }
                        return (rent.AsMemory(0, compressedLen), rent);
                    }

                default:
                    {
                        // GZip & Brotli：现代 .NET 直接支持 Span 写入，规避老旧 ToArray 的二次拷贝
                        using var ms = new MemoryStream();
                        if (algorithm == CompressionAlgorithm.GZip)
                        {
                            using (var gs = new GZipStream(ms, CompressionLevel.Optimal, true)) gs.Write(source.Span);
                        }
                        else
                        {
                            using (var bs = new BrotliStream(ms, CompressionLevel.Optimal, true)) bs.Write(source.Span);
                        }

                        var fallbackBytes = ms.ToArray();
                        return (fallbackBytes, null);
                    }
            }
        }

        private static (byte[] Buffer, int Length, byte[]? RentArray) Decompress(byte[] source, int sourceLength, int originalLength, CompressionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case CompressionAlgorithm.None:
                    // 数据本身即为序列化结果，原样返回（缓冲区由调用方 finally 归还，故 RentArray 为 null）
                    return (source, sourceLength, null);

                case CompressionAlgorithm.Lz4:
                    {
                        if (originalLength == 0) return (Array.Empty<byte>(), 0, null);

                        // 按头里记录的原始长度精确租借，杜绝越界失败与整数溢出
                        var target = ArrayPool<byte>.Shared.Rent(originalLength);
                        var decodedLen = LZ4Codec.Decode(source.AsSpan(0, sourceLength), target.AsSpan(0, target.Length));
                        if (decodedLen < 0)
                        {
                            ArrayPool<byte>.Shared.Return(target);
                            throw new InvalidOperationException("LZ4 decompression failed due to insufficient buffer size.");
                        }
                        if (decodedLen != originalLength)
                        {
                            ArrayPool<byte>.Shared.Return(target);
                            throw new InvalidOperationException($"LZ4 decompression length mismatch: expected {originalLength}, got {decodedLen}.");
                        }
                        return (target, decodedLen, target);
                    }

                default:
                    {
                        if (originalLength == 0) return (Array.Empty<byte>(), 0, null);

                        // 解压输出长度已知（头里的 originalLength），直接精确租借、把流读满，
                        // 免掉 MemoryStream.ToArray() 的精确分配（>85KB 会进 LOH）
                        var target = ArrayPool<byte>.Shared.Rent(originalLength);
                        try
                        {
                            using var input = new MemoryStream(source, 0, sourceLength);
                            using Stream ds = algorithm == CompressionAlgorithm.GZip
                                ? new GZipStream(input, CompressionMode.Decompress)
                                : new BrotliStream(input, CompressionMode.Decompress);

                            var read = 0;
                            int n;
                            while (read < originalLength && (n = ds.Read(target, read, originalLength - read)) > 0)
                                read += n;

                            return (target, read, target);
                        }
                        catch
                        {
                            ArrayPool<byte>.Shared.Return(target);
                            throw;
                        }
                    }
            }
        }
        #endregion
    }

    public enum CompressionAlgorithm
    {
        GZip,
        Brotli,
        Lz4,
        None
    }

    public enum SerializationAlgorithm
    {
        MemoryPack,
        MessagePack
    }
}
