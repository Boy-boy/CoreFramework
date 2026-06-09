using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Core.Redis
{
    public static class RedisValueConverter
    {
        private const string DateTimeIsoFormat = "o";
        private const string GuidFormat = "D";
        private const string DateOnlyFormat = "yyyy-MM-dd";
        private const string TimeOnlyFormat = "HH:mm:ss.fffffff";
        private const string TimeSpanFormat = "c";
        private const DateTimeStyles DefaultDateTimeStyles = DateTimeStyles.RoundtripKind;

        // 使用 ConcurrentDictionary 替代手动实现的 DCL，更简洁且线程安全
        private static readonly ConcurrentDictionary<Type, object> _jsonTypeInfoCache = new();
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;

        /// <summary>
        /// JsonTypeInfo 缓存条目上限。达到上限后不再缓存新类型（仍正常工作，仅性能略降），
        /// 防止动态生成类型（EF 代理、Roslyn 编译产物等）导致内存无限增长。
        /// </summary>
        public static int JsonTypeInfoCacheMaxSize { get; set; } = 1024;

        /// <summary>
        /// 当前 JsonTypeInfo 缓存条目数，供外部监控（Prometheus / OpenTelemetry 等）采集。
        /// </summary>
        public static int JsonTypeInfoCacheCount => _jsonTypeInfoCache.Count;

        /// <summary>
        /// 单条 POCO 序列化结果阈值（字节）。超过此阈值会触发 <see cref="OnLargePayload"/>。
        /// 默认 1 MiB —— Redis 反模式预警线。
        /// </summary>
        public static int LargePayloadThresholdBytes { get; set; } = 1024 * 1024;

        /// <summary>
        /// 大对象告警事件。参数：(类型, 实际字节数)。订阅方负责日志、告警、降级等动作。
        /// </summary>
        public static event Action<Type, int>? OnLargePayload;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonTypeInfo<T> GetJsonTypeInfo<T>()
        {
            if (_jsonTypeInfoCache.TryGetValue(typeof(T), out var cached))
                return (JsonTypeInfo<T>)cached;
            return GetJsonTypeInfoSlow<T>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static JsonTypeInfo<T> GetJsonTypeInfoSlow<T>()
        {
            var typeInfo = JsonOptions.GetTypeInfo(typeof(T));
            // 仅在未达上限时缓存；超过上限走"获取但不缓存"路径，避免无界增长。
            if (_jsonTypeInfoCache.Count < JsonTypeInfoCacheMaxSize)
                _jsonTypeInfoCache.TryAdd(typeof(T), typeInfo);
            return (JsonTypeInfo<T>)typeInfo;
        }

        /// <summary>
        /// 泛型序列化主入口（100% 零装箱 + JIT 编译期静态剪枝）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RedisValue ToRedisValue<T>(in T value)
        {
            // 入口统一拦截 null：引用类型 null、Nullable<T> 无值都在此短路。
            if (value is null) return RedisValue.Null;

            ref T v = ref Unsafe.AsRef(in value);

            // ---- 1. 引用类型与原生直通 ----
            if (typeof(T) == typeof(string)) return (string)(object)v!;
            if (typeof(T) == typeof(byte[])) return (byte[])(object)v!;
            if (typeof(T) == typeof(RedisValue)) return (RedisValue)(object)v;

            // ---- 2. 基础值类型 ----
            if (typeof(T) == typeof(int)) return (int)(object)v;
            if (typeof(T) == typeof(long)) return (long)(object)v;
            if (typeof(T) == typeof(bool)) return (bool)(object)v;
            if (typeof(T) == typeof(double)) return (double)(object)v;
            if (typeof(T) == typeof(float)) return (float)(object)v;
            if (typeof(T) == typeof(uint)) return (uint)(object)v;
            if (typeof(T) == typeof(ulong)) return (ulong)(object)v;

            // ---- 3. 窄整型提升 ----
            if (typeof(T) == typeof(byte)) return (int)(byte)(object)v;
            if (typeof(T) == typeof(sbyte)) return (int)(sbyte)(object)v;
            if (typeof(T) == typeof(short)) return (int)(short)(object)v;
            if (typeof(T) == typeof(ushort)) return (int)(ushort)(object)v;
            if (typeof(T) == typeof(char)) return ((char)(object)v).ToString();

            // ---- 4. Memory/Span ----
            if (typeof(T) == typeof(ReadOnlyMemory<byte>)) return (ReadOnlyMemory<byte>)(object)v;
            if (typeof(T) == typeof(Memory<byte>)) return (Memory<byte>)(object)v;

            // ---- 5. Nullable 快路径（入口已拦截 null，此处 HasValue 必为 true） ----
            if (typeof(T) == typeof(int?)) return ((int?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(long?)) return ((long?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(bool?)) return ((bool?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(double?)) return ((double?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(float?)) return ((float?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(uint?)) return ((uint?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(ulong?)) return ((ulong?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(byte?)) return (int)((byte?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(sbyte?)) return (int)((sbyte?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(short?)) return (int)((short?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(ushort?)) return (int)((ushort?)(object)v).GetValueOrDefault();
            if (typeof(T) == typeof(char?)) return ((char?)(object)v).GetValueOrDefault().ToString();

            // ---- 6. 现代文本格式值类型 ----
            if (typeof(T) == typeof(decimal)) return ((decimal)(object)v).ToString(CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(TimeSpan)) return ((TimeSpan)(object)v).ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(Guid)) return ((Guid)(object)v).ToString(GuidFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(DateTime)) return ((DateTime)(object)v).ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(DateTimeOffset)) return ((DateTimeOffset)(object)v).ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(DateOnly)) return ((DateOnly)(object)v).ToString(DateOnlyFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(TimeOnly)) return ((TimeOnly)(object)v).ToString(TimeOnlyFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(Int128)) return ((Int128)(object)v).ToString(CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(UInt128)) return ((UInt128)(object)v).ToString(CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(Half)) return ((Half)(object)v).ToString("G", CultureInfo.InvariantCulture);

            // ---- 7. 复杂值类型 Nullable（入口已拦截 null，HasValue 必为 true） ----
            if (typeof(T) == typeof(decimal?)) return ((decimal?)(object)v).GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(TimeSpan?)) return ((TimeSpan?)(object)v).GetValueOrDefault().ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(Guid?)) return ((Guid?)(object)v).GetValueOrDefault().ToString(GuidFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(DateTime?)) return ((DateTime?)(object)v).GetValueOrDefault().ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(DateTimeOffset?)) return ((DateTimeOffset?)(object)v).GetValueOrDefault().ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(DateOnly?)) return ((DateOnly?)(object)v).GetValueOrDefault().ToString(DateOnlyFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(TimeOnly?)) return ((TimeOnly?)(object)v).GetValueOrDefault().ToString(TimeOnlyFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(Int128?)) return ((Int128?)(object)v).GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(UInt128?)) return ((UInt128?)(object)v).GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(Half?)) return ((Half?)(object)v).GetValueOrDefault().ToString("G", CultureInfo.InvariantCulture);

            // ---- 8. 枚举转换（Unsafe.As 重解释字节，真正零装箱） ----
            if (typeof(T).IsEnum)
            {
                return SerializeEnum(in value);
            }

            // ---- 9. 极端罕见 Nullable 兜底 ----
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
                return ToRedisValueBoxedFallback(value);

            // ---- 10. POCO 终极慢路径 ----
            return ToRedisValueJsonFallback(in value);
        }

        /// <summary>
        /// 泛型反序列化主入口
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FromRedisValue<T>(RedisValue value)
        {
            if (value.IsNull)
            {
                if (default(T) is null) return default!;
                throw new RedisDecodeException($"无法将 Redis Null 映射为非可空的值类型: {typeof(T).FullName}");
            }

            try
            {
                // ---- 1. 原生直通 ----
                if (typeof(T) == typeof(string)) return (T)(object)(string)value!;
                if (typeof(T) == typeof(byte[])) return (T)(object)(byte[])value!;
                if (typeof(T) == typeof(RedisValue)) return (T)(object)value;

                // ---- 2. 基础值类型 ----
                if (typeof(T) == typeof(int)) return (T)(object)(int)value;
                if (typeof(T) == typeof(long)) return (T)(object)(long)value;
                if (typeof(T) == typeof(bool)) return (T)(object)(bool)value;
                if (typeof(T) == typeof(double)) return (T)(object)(double)value;
                if (typeof(T) == typeof(float)) return (T)(object)(float)value;
                if (typeof(T) == typeof(uint)) return (T)(object)(uint)value;
                if (typeof(T) == typeof(ulong)) return (T)(object)(ulong)value;

                // ---- 3. 窄整型溢出校验 ----
                if (typeof(T) == typeof(byte)) return (T)(object)checked((byte)(long)value);
                if (typeof(T) == typeof(sbyte)) return (T)(object)checked((sbyte)(long)value);
                if (typeof(T) == typeof(short)) return (T)(object)checked((short)(long)value);
                if (typeof(T) == typeof(ushort)) return (T)(object)checked((ushort)(long)value);
                if (typeof(T) == typeof(char)) return (T)(object)DeserializeChar(value);

                // ---- 4. Memory 路径 ----
                // ReadOnlyMemory：保留零拷贝，调用方约定不得修改（违反约定将污染 RedisValue 内部存储）。
                // Memory：强制拷贝，因为 Memory<byte> 暴露可写视图，与 RedisValue 内部数组共享会导致数据污染。
                if (typeof(T) == typeof(ReadOnlyMemory<byte>)) return (T)(object)(ReadOnlyMemory<byte>)value;
                if (typeof(T) == typeof(Memory<byte>))
                {
                    var src = (byte[])value!;
                    var copy = new byte[src.Length];
                    Buffer.BlockCopy(src, 0, copy, 0, src.Length);
                    return (T)(object)copy.AsMemory();
                }

                // ---- 5. Nullable（入口已拦截 IsNull，此处必有值） ----
                if (typeof(T) == typeof(int?)) return (T)(object)(int?)(int)value;
                if (typeof(T) == typeof(long?)) return (T)(object)(long?)(long)value;
                if (typeof(T) == typeof(bool?)) return (T)(object)(bool?)(bool)value;
                if (typeof(T) == typeof(double?)) return (T)(object)(double?)(double)value;
                if (typeof(T) == typeof(float?)) return (T)(object)(float?)(float)value;
                if (typeof(T) == typeof(uint?)) return (T)(object)(uint?)(uint)value;
                if (typeof(T) == typeof(ulong?)) return (T)(object)(ulong?)(ulong)value;
                if (typeof(T) == typeof(byte?)) return (T)(object)(byte?)checked((byte)(long)value);
                if (typeof(T) == typeof(sbyte?)) return (T)(object)(sbyte?)checked((sbyte)(long)value);
                if (typeof(T) == typeof(short?)) return (T)(object)(short?)checked((short)(long)value);
                if (typeof(T) == typeof(ushort?)) return (T)(object)(ushort?)checked((ushort)(long)value);
                if (typeof(T) == typeof(char?)) return (T)(object)(char?)DeserializeChar(value);

                // ---- 6. 文本格式值类型 ----
                if (typeof(T) == typeof(decimal)) return (T)(object)decimal.Parse((string)value!, NumberStyles.Number, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(TimeSpan)) return (T)(object)TimeSpan.ParseExact((string)value!, TimeSpanFormat, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(Guid)) return (T)(object)Guid.Parse((string)value!);
                if (typeof(T) == typeof(DateTime)) return (T)(object)ParseDateTime((string)value!);
                if (typeof(T) == typeof(DateTimeOffset)) return (T)(object)ParseDateTimeOffset((string)value!);
                if (typeof(T) == typeof(DateOnly)) return (T)(object)DateOnly.ParseExact((string)value!, DateOnlyFormat, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(TimeOnly)) return (T)(object)TimeOnly.ParseExact((string)value!, TimeOnlyFormat, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(Int128)) return (T)(object)Int128.Parse((string)value!, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(UInt128)) return (T)(object)UInt128.Parse((string)value!, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(Half)) return (T)(object)Half.Parse((string)value!, CultureInfo.InvariantCulture);

                // ---- 7. 复杂值类型 Nullable（入口已拦截 IsNull） ----
                if (typeof(T) == typeof(decimal?)) return (T)(object)(decimal?)decimal.Parse((string)value!, NumberStyles.Number, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(TimeSpan?)) return (T)(object)(TimeSpan?)TimeSpan.ParseExact((string)value!, TimeSpanFormat, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(Guid?)) return (T)(object)(Guid?)Guid.Parse((string)value!);
                if (typeof(T) == typeof(DateTime?)) return (T)(object)(DateTime?)ParseDateTime((string)value!);
                if (typeof(T) == typeof(DateTimeOffset?)) return (T)(object)(DateTimeOffset?)ParseDateTimeOffset((string)value!);
                if (typeof(T) == typeof(DateOnly?)) return (T)(object)(DateOnly?)DateOnly.ParseExact((string)value!, DateOnlyFormat, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(TimeOnly?)) return (T)(object)(TimeOnly?)TimeOnly.ParseExact((string)value!, TimeOnlyFormat, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(Int128?)) return (T)(object)(Int128?)Int128.Parse((string)value!, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(UInt128?)) return (T)(object)(UInt128?)UInt128.Parse((string)value!, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(Half?)) return (T)(object)(Half?)Half.Parse((string)value!, CultureInfo.InvariantCulture);

                // ---- 8. 枚举类型（Unsafe.As 重解释字节，真正零装箱） ----
                if (typeof(T).IsEnum) return DeserializeEnum<T>(value);

                // ---- 9. 稀有 Nullable 兜底 ----
                Type? underlying = Nullable.GetUnderlyingType(typeof(T));
                if (underlying != null) return (T)FromRedisValueBoxedFallback(underlying, value)!;

                // ---- 10. POCO 终极慢路径 ----
                return FromRedisValueJsonFallback<T>(value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                                       and not OutOfMemoryException
                                       and not RedisDecodeException)
            {
                throw new RedisDecodeException(
                    $"反序列化 RedisValue 到类型 '{typeof(T).FullName}' 失败。RedisValue 预览: {GetRedisValuePreview(value)}",
                    ex);
            }
        }

        /// <summary>
        /// 截取 RedisValue 内容用于异常诊断。最长 256 字符，超长追加 "...(truncated, total {N})"。
        /// 任何提取失败统一降级为占位串，绝不抛出新异常掩盖原始错误。
        /// 注意：可能包含敏感数据，调用方在日志写出前自行决定是否脱敏。
        /// </summary>
        private static string GetRedisValuePreview(RedisValue value, int maxLength = 256)
        {
            try
            {
                if (value.IsNull) return "<null>";
                string? s = (string?)value;
                if (s is null) return "<null>";
                if (s.Length <= maxLength) return $"'{s}'";
                return $"'{s.AsSpan(0, maxLength).ToString()}'...(truncated, total {s.Length})";
            }
            catch
            {
                return "<unable to preview>";
            }
        }

        #region 内部私有高内联辅助块

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char DeserializeChar(RedisValue value)
        {
            var s = (string)value!;

            if (string.IsNullOrEmpty(s))
                throw new RedisDecodeException("反序列化失败：无法将空字符串转换为 Char");

            // 严格检查：字符串长度必须为 1
            if (s.Length > 1)
                throw new RedisDecodeException($"反序列化失败：字符串 '{s}' 长度超过 1，无法转换为 Char");

            return s[0];
        }

        /// <summary>
        /// 枚举序列化 —— 通过 Unsafe.As 重解释字节，避免 (object)enum 装箱。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RedisValue SerializeEnum<T>(in T value)
        {
            ref T r = ref Unsafe.AsRef(in value);
            return Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Int32 => Unsafe.As<T, int>(ref r),
                TypeCode.Int64 => Unsafe.As<T, long>(ref r),
                TypeCode.UInt32 => Unsafe.As<T, uint>(ref r),
                TypeCode.UInt64 => Unsafe.As<T, ulong>(ref r),
                TypeCode.Int16 => (int)Unsafe.As<T, short>(ref r),
                TypeCode.UInt16 => (int)Unsafe.As<T, ushort>(ref r),
                TypeCode.Byte => (int)Unsafe.As<T, byte>(ref r),
                TypeCode.SByte => (int)Unsafe.As<T, sbyte>(ref r),
                _ => throw new NotSupportedException($"不支持的枚举基础布局格式: {Enum.GetUnderlyingType(typeof(T))}")
            };
        }

        /// <summary>
        /// 枚举反序列化 —— 通过 Unsafe.As 重解释字节，避免 (T)(object)int 装箱。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T DeserializeEnum<T>(RedisValue value)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32: { int n = (int)value; return Unsafe.As<int, T>(ref n); }
                case TypeCode.Int64: { long n = (long)value; return Unsafe.As<long, T>(ref n); }
                case TypeCode.UInt32: { uint n = (uint)value; return Unsafe.As<uint, T>(ref n); }
                case TypeCode.UInt64: { ulong n = (ulong)value; return Unsafe.As<ulong, T>(ref n); }
                case TypeCode.Int16: { short n = checked((short)(long)value); return Unsafe.As<short, T>(ref n); }
                case TypeCode.UInt16: { ushort n = checked((ushort)(long)value); return Unsafe.As<ushort, T>(ref n); }
                case TypeCode.Byte: { byte n = checked((byte)(long)value); return Unsafe.As<byte, T>(ref n); }
                case TypeCode.SByte: { sbyte n = checked((sbyte)(long)value); return Unsafe.As<sbyte, T>(ref n); }
                default: throw new NotSupportedException($"不支持的枚举基础布局格式: {Enum.GetUnderlyingType(typeof(T))}");
            }
        }

        private static DateTime ParseDateTime(string s)
        {
            if (DateTime.TryParseExact(s, DateTimeIsoFormat, CultureInfo.InvariantCulture, DefaultDateTimeStyles, out var dt))
                return dt;
            return DateTime.Parse(s, CultureInfo.InvariantCulture, DefaultDateTimeStyles);
        }

        private static DateTimeOffset ParseDateTimeOffset(string s)
        {
            if (DateTimeOffset.TryParseExact(s, DateTimeIsoFormat, CultureInfo.InvariantCulture, DefaultDateTimeStyles, out var dto))
                return dto;
            return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DefaultDateTimeStyles);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RedisValue ToRedisValueJsonFallback<T>(in T value)
        {
            byte[] bytes;
            try
            {
                bytes = JsonSerializer.SerializeToUtf8Bytes(value, GetJsonTypeInfo<T>());
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                                       and not OutOfMemoryException
                                       and not RedisEncodeException)
            {
                throw new RedisEncodeException($"序列化类型 '{typeof(T).FullName}' 到 RedisValue 失败。", ex);
            }

            // 大对象告警 —— 仅当订阅了事件时才走这条路径，避免 hot path 上的 volatile 读。
            var handler = OnLargePayload;
            if (handler is not null && bytes.Length >= LargePayloadThresholdBytes)
            {
                try { handler(typeof(T), bytes.Length); }
                catch { /* 订阅方异常不影响主流程 */ }
            }
            return bytes;
        }

        /// <summary>
        /// POCO 慢路径反序列化。直接走 RedisValue → ReadOnlyMemory&lt;byte&gt; 的 explicit operator：
        /// Raw 存储零拷贝，整数/字符串存储由 RedisValue 自身编码为字节序列。
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T FromRedisValueJsonFallback<T>(RedisValue value)
        {
            ReadOnlyMemory<byte> memory = (ReadOnlyMemory<byte>)value;
            return JsonSerializer.Deserialize<T>(memory.Span, GetJsonTypeInfo<T>())!;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RedisValue ToRedisValueBoxedFallback(object value) => value switch
        {
            int i => i,
            long l => l,
            bool b => b,
            double d => d,
            float f => f,
            uint u => u,
            ulong ul => ul,
            byte b8 => (int)b8,
            sbyte sb => (int)sb,
            short s16 => (int)s16,
            ushort us16 => (int)us16,
            char c => c.ToString(),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString(TimeSpanFormat, CultureInfo.InvariantCulture),
            Guid g => g.ToString(GuidFormat, CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture),
            DateOnly d => d.ToString(DateOnlyFormat, CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString(TimeOnlyFormat, CultureInfo.InvariantCulture),
            Int128 i128 => i128.ToString(CultureInfo.InvariantCulture),
            UInt128 u128 => u128.ToString(CultureInfo.InvariantCulture),
            Half h => h.ToString("G", CultureInfo.InvariantCulture),
            _ when value.GetType().IsEnum => SerializeEnumBoxed(value),
            _ => JsonSerializer.SerializeToUtf8Bytes(value, value.GetType())
        };

        private static RedisValue SerializeEnumBoxed(object value)
        {
            Type underlying = Enum.GetUnderlyingType(value.GetType());
            return underlying == typeof(ulong) ? Convert.ToUInt64(value) : Convert.ToInt64(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object FromRedisValueBoxedFallback(Type t, RedisValue value)
        {
            if (t == typeof(int)) return (int)value;
            if (t == typeof(long)) return (long)value;
            if (t == typeof(bool)) return (bool)value;
            if (t == typeof(double)) return (double)value;
            if (t == typeof(float)) return (float)value;
            if (t == typeof(uint)) return (uint)value;
            if (t == typeof(ulong)) return (ulong)value;
            if (t == typeof(byte)) return checked((byte)(long)value);
            if (t == typeof(sbyte)) return checked((sbyte)(long)value);
            if (t == typeof(short)) return checked((short)(long)value);
            if (t == typeof(ushort)) return checked((ushort)(long)value);
            if (t == typeof(char)) return DeserializeChar(value);
            if (t == typeof(decimal)) return decimal.Parse((string)value!, NumberStyles.Number, CultureInfo.InvariantCulture);
            if (t == typeof(TimeSpan)) return TimeSpan.ParseExact((string)value!, TimeSpanFormat, CultureInfo.InvariantCulture);
            if (t == typeof(Guid)) return Guid.Parse((string)value!);
            if (t == typeof(DateTime)) return ParseDateTime((string)value!);
            if (t == typeof(DateTimeOffset)) return ParseDateTimeOffset((string)value!);
            if (t == typeof(DateOnly)) return DateOnly.ParseExact((string)value!, DateOnlyFormat, CultureInfo.InvariantCulture);
            if (t == typeof(TimeOnly)) return TimeOnly.ParseExact((string)value!, TimeOnlyFormat, CultureInfo.InvariantCulture);
            if (t == typeof(Int128)) return Int128.Parse((string)value!, CultureInfo.InvariantCulture);
            if (t == typeof(UInt128)) return UInt128.Parse((string)value!, CultureInfo.InvariantCulture);
            if (t == typeof(Half)) return Half.Parse((string)value!, CultureInfo.InvariantCulture);
            if (t.IsEnum)
            {
                return Type.GetTypeCode(t) == TypeCode.UInt64
                    ? Enum.ToObject(t, (ulong)value)
                    : Enum.ToObject(t, (long)value);
            }
            return JsonSerializer.Deserialize(((ReadOnlyMemory<byte>)value).Span, t);
        }

        #endregion
    }

    public class RedisDecodeException : Exception
    {
        public RedisDecodeException(string message) : base(message) { }
        public RedisDecodeException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class RedisEncodeException : Exception
    {
        public RedisEncodeException(string message) : base(message) { }
        public RedisEncodeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
