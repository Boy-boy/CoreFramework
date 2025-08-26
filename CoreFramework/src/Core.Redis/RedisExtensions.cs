using K4os.Compression.LZ4;
using MemoryPack;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace Core.Redis
{
    public static class RedisExtensions
    {
        public static async Task SetLargeObjectAsync<T>(this IRedisCache redis, string key, T value,
            TimeSpan? expiry = null, int db = -1, CompressionAlgorithm algorithm = CompressionAlgorithm.Lz4,
            SerializationAlgorithm serializationAlgorithm = SerializationAlgorithm.MessagePack)
        {
            var stackExchangeRedis = redis as StackExchangeRedis;
            await stackExchangeRedis.SetLargeObjectAsync(key, value, expiry, db, algorithm, serializationAlgorithm);
        }

        public static async Task<T> GetLargeObjectAsync<T>(this IRedisCache redis, string key,
            int db = -1, CompressionAlgorithm algorithm = CompressionAlgorithm.Lz4,
            SerializationAlgorithm serializationAlgorithm = SerializationAlgorithm.MessagePack)
        {
            var stackExchangeRedis = redis as StackExchangeRedis;
            return await stackExchangeRedis.GetLargeObjectAsync<T>(key, db, algorithm, serializationAlgorithm);
        }

        private const int ChunkSize = 1 * 1024 * 1024; // 10MB per chunk

        /// <summary>
        /// 将大对象分片并压缩后存储到 Redis
        /// </summary>
        public static async Task SetLargeObjectAsync<T>(this StackExchangeRedis redis, string key, T value,
            TimeSpan? expiry = null, int db = -1, CompressionAlgorithm algorithm = CompressionAlgorithm.Lz4,
            SerializationAlgorithm serializationAlgorithm = SerializationAlgorithm.MessagePack)
        {
            if (redis == null) throw new ArgumentNullException(nameof(redis));
            if (key == null) throw new ArgumentNullException(nameof(key));
            var prefixedKey = redis.Options.GetPrefixedKey(key);
            try
            {
                var database = redis.GetDatabase(db);

                // 清理旧的分片数据
                await CleanupOldChunksAsync(database, prefixedKey).ConfigureAwait(false);

                // 序列化并压缩数据
                var serializedValue = Serialize(value, serializationAlgorithm);
                var compressedValue = Compress(serializedValue, algorithm);
                var chunks = SplitIntoChunks(compressedValue, ChunkSize);

                // 创建管道
                var batch = database.CreateBatch();

                // 批量设置键值对和过期时间
                var tasks = new List<Task>();
                foreach (var (chunk, index) in chunks.Select((chunk, index) => (chunk, index)))
                {
                    var chunkKey = GetChunkKey(prefixedKey, index);
                    tasks.Add(batch.StringSetAsync(chunkKey, chunk, expiry));
                }

                // 存储分片元数据
                tasks.Add(database.StringSetAsync(GetChunkCount(prefixedKey), chunks.Count, expiry));

                // 执行管道
                batch.Execute();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                redis.Logger.LogError(ex, "Failed to set large object for key: {Key}", key);
                throw new InvalidOperationException($"Failed to set large object for key: {key}", ex);
            }
        }

        /// <summary>
        /// 从 Redis 中获取分片并解压缩后的大对象
        /// </summary>
        public static async Task<T> GetLargeObjectAsync<T>(this StackExchangeRedis redis, string key,
            int db = -1, CompressionAlgorithm algorithm = CompressionAlgorithm.Lz4,
            SerializationAlgorithm serializationAlgorithm = SerializationAlgorithm.MessagePack)
        {
            if (redis == null) throw new ArgumentNullException(nameof(redis));
            if (key == null) throw new ArgumentNullException(nameof(key));
            var prefixedKey = redis.Options.GetPrefixedKey(key);
            try
            {
                var database = redis.GetDatabase(db);

                // 获取分片数量
                var chunkCount = (int)await database.StringGetAsync(GetChunkCount(prefixedKey)).ConfigureAwait(false);
                if (chunkCount == 0)
                    return default;

                // 使用 ArrayPool 减少内存分配
                var chunks = ArrayPool<byte[]>.Shared.Rent(chunkCount);
                try
                {
                    // 并行获取所有分片数据
                    var tasks = new Task<RedisValue>[chunkCount];
                    var batch = database.CreateBatch();
                    for (var i = 0; i < chunkCount; i++)
                    {
                        var chunkKey = GetChunkKey(prefixedKey, i);
                        tasks[i] = batch.StringGetAsync(chunkKey);
                    }
                    batch.Execute();

                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    // 检查分片数据是否完整
                    for (var i = 0; i < chunkCount; i++)
                    {
                        if (tasks[i].Result.IsNullOrEmpty)
                            throw new InvalidOperationException($"Missing chunk {i} for key: {key}");
                        chunks[i] = tasks[i].Result;
                    }

                    // 流式合并分片并解压缩
                    var compressedData = CombineChunks(chunks);
                    var decompressionData = Decompress(compressedData, algorithm);

                    // 反序列化
                    return Deserialize<T>(decompressionData, serializationAlgorithm);
                }
                finally
                {
                    // 归还 ArrayPool 中的数组
                    ArrayPool<byte[]>.Shared.Return(chunks);
                }
            }
            catch (Exception ex)
            {
                redis.Logger.LogError(ex, "Failed to get large object for key: {Key}", key);
                throw new InvalidOperationException($"Failed to get large object for key: {key}", ex);
            }
        }

        /// <summary>
        /// 清理旧的分片数据
        /// </summary>
        private static async Task CleanupOldChunksAsync(IDatabase database, string key)
        {
            var chunkCount = (int)await database.StringGetAsync(GetChunkCount(key)).ConfigureAwait(false);
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 };
            Parallel.For(0, chunkCount, options, i =>
            {
                database.KeyDelete(GetChunkKey(key, i));
            });
            database.KeyDelete(GetChunkCount(key));
        }

        /// <summary>
        /// 获取分片键
        /// </summary>
        private static string GetChunkKey(string key, int index)
        {
            var safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
            return $"{safeKey}:chunk:{index}";
        }

        private static string GetChunkCount(string key)
        {
            var safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
            return $"{safeKey}:chunkCount";
        }

        /// <summary>
        /// 将数据分片
        /// </summary>
        private static List<byte[]> SplitIntoChunks(byte[] data, int chunkSize)
        {
            var chunks = new List<byte[]>();
            for (var i = 0; i < data.Length; i += chunkSize)
            {
                var chunk = new byte[Math.Min(chunkSize, data.Length - i)];
                Array.Copy(data, i, chunk, 0, chunk.Length);
                chunks.Add(chunk);
            }
            return chunks;
        }

        /// <summary>
        /// 合并分片
        /// </summary>
        private static byte[] CombineChunks(byte[][] chunks)
        {
            // 检查 chunks 是否为空
            if (chunks == null || chunks.Length == 0)
                return Array.Empty<byte>();

            // 计算总长度
            var totalLength = 0;
            foreach (var chunk in chunks)
            {
                if (chunk != null)
                    totalLength += chunk.Length;
            }

            // 如果总长度为 0，返回空数组
            if (totalLength == 0)
                return Array.Empty<byte>();

            // 创建结果数组
            var result = new byte[totalLength];
            var offset = 0;

            // 合并分片数据
            foreach (var chunk in chunks)
            {
                if (chunk == null || chunk.Length <= 0)
                    continue;

                Array.Copy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            return result;
        }

        /// <summary>
        /// 压缩数据
        /// </summary>
        private static byte[] Compress(byte[] data, CompressionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case CompressionAlgorithm.GZip:
                    using (var outputStream = new MemoryStream())
                    {
                        using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                        {
                            compressionStream.Write(data, 0, data.Length);
                        }
                        return outputStream.ToArray();
                    }

                case CompressionAlgorithm.Brotli:
                    using (var outputStream = new MemoryStream())
                    {
                        using (var compressionStream = new BrotliStream(outputStream, CompressionLevel.Optimal))
                        {
                            compressionStream.Write(data, 0, data.Length);
                        }
                        return outputStream.ToArray();
                    }

                case CompressionAlgorithm.Lz4:
                    return LZ4Pickler.Pickle(data);

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
        }

        /// <summary>
        /// 解压缩数据
        /// </summary>
        private static byte[] Decompress(byte[] compressedData, CompressionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case CompressionAlgorithm.GZip:
                    using (var inputStream = new MemoryStream(compressedData))
                    using (var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    using (var outputStream = new MemoryStream())
                    {
                        decompressionStream.CopyTo(outputStream);
                        return outputStream.ToArray();
                    }

                case CompressionAlgorithm.Brotli:
                    using (var inputStream = new MemoryStream(compressedData))
                    using (var decompressionStream = new BrotliStream(inputStream, CompressionMode.Decompress))
                    using (var outputStream = new MemoryStream())
                    {
                        decompressionStream.CopyTo(outputStream);
                        return outputStream.ToArray();
                    }

                case CompressionAlgorithm.Lz4:
                    return LZ4Pickler.Unpickle(compressedData);

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
        }

        /// <summary>
        /// 序列化数据
        /// </summary>
        private static byte[] Serialize<T>(T value, SerializationAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case SerializationAlgorithm.MemoryPack:
                    return MemoryPackSerializer.Serialize(value);
                case SerializationAlgorithm.MessagePack:
                    return SerializationHelper.Serialize(value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
        }

        /// <summary>
        /// 反序列化数据
        /// </summary>
        private static T Deserialize<T>(byte[] data, SerializationAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case SerializationAlgorithm.MemoryPack:
                    return MemoryPackSerializer.Deserialize<T>(data);
                case SerializationAlgorithm.MessagePack:
                    return SerializationHelper.Deserialize<T>(data);
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
        }
    }

    public enum CompressionAlgorithm
    {
        GZip,
        Brotli,
        Lz4
    }

    public enum SerializationAlgorithm
    {
        MemoryPack,
        MessagePack
    }
}
