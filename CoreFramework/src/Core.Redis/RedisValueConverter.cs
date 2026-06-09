using StackExchange.Redis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Core.Redis
{
    /// <summary>
    /// .NET 对象与 <see cref="RedisValue"/> 的双向转换器。
    /// 特性：JIT 静态分派（死分支剪枝）、零装箱（含 Enum / char / 常用 Nullable）、POCO 零拷贝、溢出与截断校验、完善异常诊断。
    /// </summary>
    public static class RedisValueConverter
    {
        private const string GuidFormat = "D";
        private const string DateTimeIsoFormat = "o";
        private const DateTimeStyles DefaultDateTimeStyles = DateTimeStyles.RoundtripKind;

        /// <summary>
        /// 将泛型对象高效转为 RedisValue
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RedisValue ToRedisValue<T>(T value)
        {
            // 纯值类型在 JIT 单态化时 value is null 折成静态 false，整段会被裁掉
            if (value is null) return RedisValue.Null;

            // ---- 引用类型 ----
            if (typeof(T) == typeof(string)) return (string)(object)value;
            if (typeof(T) == typeof(byte[])) return (byte[])(object)value;
            if (typeof(T) == typeof(RedisValue)) return (RedisValue)(object)value;

            // ---- RedisValue 自带 implicit operator 的基础值类型 ----
            if (typeof(T) == typeof(int)) return (int)(object)value;
            if (typeof(T) == typeof(long)) return (long)(object)value;
            if (typeof(T) == typeof(bool)) return (bool)(object)value;
            if (typeof(T) == typeof(double)) return (double)(object)value;
            if (typeof(T) == typeof(float)) return (float)(object)value;
            if (typeof(T) == typeof(uint)) return (uint)(object)value;
            if (typeof(T) == typeof(ulong)) return (ulong)(object)value;

            // ---- byte/sbyte/short/ushort：中转为 int ----
            if (typeof(T) == typeof(byte)) return (int)(byte)(object)value;
            if (typeof(T) == typeof(sbyte)) return (sbyte)(object)value;
            if (typeof(T) == typeof(short)) return (short)(object)value;
            if (typeof(T) == typeof(ushort)) return (int)(ushort)(object)value;

            // ---- char 特化：Unsafe.As 重解释，零装箱 ----
            if (typeof(T) == typeof(char)) return Unsafe.As<T, char>(ref value).ToString();

            // ---- Enum 特化路径：零装箱，避免走高能耗的 POCO 序列化 ----
            if (typeof(T).IsEnum)
                return SerializeEnum(value);

            // ---- 走文本格式存储的值类型 ----
            if (typeof(T) == typeof(decimal))
                return ((decimal)(object)value).ToString(CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(TimeSpan))
                return ((TimeSpan)(object)value).ToString(); // 修复：TimeSpan 无需传入且不支持单 CultureInfo 参数
            if (typeof(T) == typeof(Guid))
                return ((Guid)(object)value).ToString(GuidFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(DateTime))
                return ((DateTime)(object)value).ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(DateTimeOffset))
                return ((DateTimeOffset)(object)value).ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture);

            // ---- 内存切片 （零拷贝直通） ----
            if (typeof(T) == typeof(ReadOnlyMemory<byte>)) return (ReadOnlyMemory<byte>)(object)value;
            if (typeof(T) == typeof(Memory<byte>)) return (Memory<byte>)(object)value;

            // ---- 常用 Nullable 快路径：零装箱（已通过顶部 value is null 守卫，HasValue 必为 true）----
            if (typeof(T) == typeof(int?)) return Unsafe.As<T, int?>(ref value).GetValueOrDefault();
            if (typeof(T) == typeof(long?)) return Unsafe.As<T, long?>(ref value).GetValueOrDefault();
            if (typeof(T) == typeof(bool?)) return Unsafe.As<T, bool?>(ref value).GetValueOrDefault();
            if (typeof(T) == typeof(double?)) return Unsafe.As<T, double?>(ref value).GetValueOrDefault();

            // ---- Nullable<U> 慢路径 ----
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
                return ToRedisValueBoxed(value);

            // ---- POCO 零拷贝优化：直接返回 ReadOnlyMemory<byte> ----
            return SerializationHelper.Serialize(value);
        }

        /// <summary>
        /// 将 RedisValue 反向还原为泛型对象
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FromRedisValue<T>(RedisValue value)
        {
            if (value.IsNull)
                return default;

            // ---- 引用类型 ----（直接 typeof(T) 触发 JIT 静态分支剪裁，零运行时开销）
            if (typeof(T) == typeof(string)) return (T)(object)(string)value;
            if (typeof(T) == typeof(byte[])) return (T)(object)(byte[])value;
            if (typeof(T) == typeof(RedisValue)) return (T)(object)value;

            try
            {
                // ---- 基础整型 ----
                if (typeof(T) == typeof(int)) return (T)(object)(int)value;
                if (typeof(T) == typeof(long)) return (T)(object)(long)value;
                if (typeof(T) == typeof(bool)) return (T)(object)(bool)value;
                if (typeof(T) == typeof(double)) return (T)(object)(double)value;
                if (typeof(T) == typeof(float)) return (T)(object)(float)value;
                if (typeof(T) == typeof(uint)) return (T)(object)(uint)value;
                if (typeof(T) == typeof(ulong)) return (T)(object)(ulong)value;

                // ---- 窄整型：提供安全的越界防御 ----
                if (typeof(T) == typeof(byte))
                {
                    int intVal = (int)value;
                    if (intVal < byte.MinValue || intVal > byte.MaxValue)
                        throw new OverflowException($"数值 {intVal} 超出 byte 类型范围 [{byte.MinValue}, {byte.MaxValue}]");
                    return (T)(object)(byte)intVal;
                }
                if (typeof(T) == typeof(sbyte))
                {
                    int intVal = (int)value;
                    if (intVal < sbyte.MinValue || intVal > sbyte.MaxValue)
                        throw new OverflowException($"数值 {intVal} 超出 sbyte 类型范围 [{sbyte.MinValue}, {sbyte.MaxValue}]");
                    return (T)(object)(sbyte)intVal;
                }
                if (typeof(T) == typeof(short))
                {
                    int intVal = (int)value;
                    if (intVal < short.MinValue || intVal > short.MaxValue)
                        throw new OverflowException($"数值 {intVal} 超出 short 类型范围 [{short.MinValue}, {short.MaxValue}]");
                    return (T)(object)(short)intVal;
                }
                if (typeof(T) == typeof(ushort))
                {
                    int intVal = (int)value;
                    if (intVal < ushort.MinValue || intVal > ushort.MaxValue)
                        throw new OverflowException($"数值 {intVal} 超出 ushort 类型范围 [{ushort.MinValue}, {ushort.MaxValue}]");
                    return (T)(object)(ushort)intVal;
                }

                // ---- Char 类型：严格禁止多字符截断隐患（Unsafe.As 写回，零装箱） ----
                if (typeof(T) == typeof(char))
                {
                    string strVal = (string)value;
                    if (string.IsNullOrEmpty(strVal)) return default;
                    if (strVal.Length > 1)
                        throw new FormatException($"字符串「{strVal}」长度大于1，无法安全转换为 char 类型");
                    char c = strVal[0];
                    return Unsafe.As<char, T>(ref c);
                }

                // ---- 枚举反序列化 ----
                if (typeof(T).IsEnum)
                    return DeserializeEnum<T>(value);

                // ---- 文本协议值类型解析（DateTime 系列使用 ParseExact 跳过格式探测） ----
                if (typeof(T) == typeof(decimal)) return (T)(object)decimal.Parse((string)value, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(TimeSpan)) return (T)(object)TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(Guid)) return (T)(object)Guid.Parse((string)value);
                if (typeof(T) == typeof(DateTime)) return (T)(object)DateTime.ParseExact((string)value, DateTimeIsoFormat, CultureInfo.InvariantCulture, DefaultDateTimeStyles);
                if (typeof(T) == typeof(DateTimeOffset)) return (T)(object)DateTimeOffset.ParseExact((string)value, DateTimeIsoFormat, CultureInfo.InvariantCulture, DefaultDateTimeStyles);

                // ---- 内存切片：彻底规避数组克隆 ----
                if (typeof(T) == typeof(ReadOnlyMemory<byte>)) return (T)(object)(ReadOnlyMemory<byte>)value;
                if (typeof(T) == typeof(Memory<byte>)) return (T)(object)new Memory<byte>((byte[])value);

                // ---- 常用 Nullable 快路径：零装箱写回 ----
                if (typeof(T) == typeof(int?)) { int? nv = (int)value; return Unsafe.As<int?, T>(ref nv); }
                if (typeof(T) == typeof(long?)) { long? nv = (long)value; return Unsafe.As<long?, T>(ref nv); }
                if (typeof(T) == typeof(bool?)) { bool? nv = (bool)value; return Unsafe.As<bool?, T>(ref nv); }
                if (typeof(T) == typeof(double?)) { double? nv = (double)value; return Unsafe.As<double?, T>(ref nv); }

                // ---- 可空类型兜底 慢路径 ----
                Type underlying = Nullable.GetUnderlyingType(typeof(T));
                if (underlying != null)
                    return (T)FromRedisValueBoxed(underlying, value);

                // ---- 自定义 POCO 对象 ----
                return SerializationHelper.Deserialize<T>((ReadOnlyMemory<byte>)value);
            }
            catch (Exception ex) when (!(ex is SerializationException))
            {
                // 统一异常屏障：捕获格式、越界、及自定义序列化器的破坏性崩溃，包装为诊断友好的标准异常
                throw new SerializationException($"将 RedisValue 转换为目标泛型类型 {typeof(T).FullName} 失败，详见内部异常细节。", ex);
            }
        }

        #region 慢路径与辅助方法

        /// <summary>
        /// Enum 序列化快路径：通过 Unsafe.As 重解释底层比特位，彻底消除装箱分配。
        /// switch 已覆盖 CLR 允许的 8 种枚举底层类型，default 仅作为防御性死分支。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RedisValue SerializeEnum<T>(T value)
        {
            return Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Int32 => Unsafe.As<T, int>(ref value),
                TypeCode.Int64 => Unsafe.As<T, long>(ref value),
                TypeCode.UInt32 => Unsafe.As<T, uint>(ref value),
                TypeCode.UInt64 => Unsafe.As<T, ulong>(ref value),
                TypeCode.Int16 => (int)Unsafe.As<T, short>(ref value),
                TypeCode.UInt16 => (int)Unsafe.As<T, ushort>(ref value),
                TypeCode.Byte => (int)Unsafe.As<T, byte>(ref value),
                TypeCode.SByte => (int)Unsafe.As<T, sbyte>(ref value),
                _ => throw new NotSupportedException($"不支持的枚举底层类型: {Enum.GetUnderlyingType(typeof(T))}")
            };
        }

        /// <summary>
        /// Enum 反序列化快路径：先按底层类型解码 RedisValue，再 Unsafe.As 写回 T，零装箱、零 Enum.ToObject 反射。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T DeserializeEnum<T>(RedisValue value)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32: { int v = (int)value; return Unsafe.As<int, T>(ref v); }
                case TypeCode.Int64: { long v = (long)value; return Unsafe.As<long, T>(ref v); }
                case TypeCode.UInt32: { uint v = (uint)value; return Unsafe.As<uint, T>(ref v); }
                case TypeCode.UInt64: { ulong v = (ulong)value; return Unsafe.As<ulong, T>(ref v); }
                case TypeCode.Int16: { short v = (short)(int)value; return Unsafe.As<short, T>(ref v); }
                case TypeCode.UInt16: { ushort v = (ushort)(int)value; return Unsafe.As<ushort, T>(ref v); }
                case TypeCode.Byte: { byte v = (byte)(int)value; return Unsafe.As<byte, T>(ref v); }
                case TypeCode.SByte: { sbyte v = (sbyte)(int)value; return Unsafe.As<sbyte, T>(ref v); }
                default:
                    throw new NotSupportedException($"不支持的枚举底层类型: {Enum.GetUnderlyingType(typeof(T))}");
            }
        }

        private static RedisValue ToRedisValueBoxed(object value) => value switch
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
            TimeSpan ts => ts.ToString(),
            Guid g => g.ToString(GuidFormat, CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString(DateTimeIsoFormat, CultureInfo.InvariantCulture),
            // 修复：原 SerializeEnum(value) 在 T=object 下 typeof switch 全 miss，落入 Convert.ToInt64，
            // 对 ulong-backed 枚举且数值 > long.MaxValue 时会 OverflowException。改走专用 boxed 分派。
            _ when value.GetType().IsEnum => SerializeEnumBoxed(value),
            _ => SerializationHelper.Serialize(value)
        };

        /// <summary>
        /// 装箱场景下的枚举序列化：按 underlying type 分派，ulong-backed 走 UInt64 通道避免溢出。
        /// </summary>
        private static RedisValue SerializeEnumBoxed(object value)
        {
            Type underlying = Enum.GetUnderlyingType(value.GetType());
            return underlying == typeof(ulong)
                ? Convert.ToUInt64(value)
                : Convert.ToInt64(value);
        }

        private static object FromRedisValueBoxed(Type t, RedisValue value)
        {
            if (t == typeof(int)) return (int)value;
            if (t == typeof(long)) return (long)value;
            if (t == typeof(bool)) return (bool)value;
            if (t == typeof(double)) return (double)value;
            if (t == typeof(float)) return (float)value;
            if (t == typeof(uint)) return (uint)value;
            if (t == typeof(ulong)) return (ulong)value;

            if (t == typeof(byte))
            {
                int val = (int)value;
                if (val < byte.MinValue || val > byte.MaxValue) throw new OverflowException($"数值 {val} 超出 byte 范围");
                return (byte)val;
            }
            if (t == typeof(sbyte))
            {
                int val = (int)value;
                if (val < sbyte.MinValue || val > sbyte.MaxValue) throw new OverflowException($"数值 {val} 超出 sbyte 范围");
                return (sbyte)val;
            }
            if (t == typeof(short))
            {
                int val = (int)value;
                if (val < short.MinValue || val > short.MaxValue) throw new OverflowException($"数值 {val} 超出 short 范围");
                return (short)val;
            }
            if (t == typeof(ushort))
            {
                int val = (int)value;
                if (val < ushort.MinValue || val > ushort.MaxValue) throw new OverflowException($"数值 {val} 超出 ushort 范围");
                return (ushort)val;
            }

            if (t == typeof(char))
            {
                string s = (string)value;
                if (string.IsNullOrEmpty(s)) return default(char);
                if (s.Length > 1) throw new FormatException("字符串长度大于1，无法转为 char");
                return s[0];
            }

            if (t == typeof(decimal)) return decimal.Parse((string)value, CultureInfo.InvariantCulture);
            if (t == typeof(TimeSpan)) return TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
            if (t == typeof(Guid)) return Guid.Parse((string)value);
            if (t == typeof(DateTime)) return DateTime.ParseExact((string)value, DateTimeIsoFormat, CultureInfo.InvariantCulture, DefaultDateTimeStyles);
            if (t == typeof(DateTimeOffset)) return DateTimeOffset.ParseExact((string)value, DateTimeIsoFormat, CultureInfo.InvariantCulture, DefaultDateTimeStyles);

            // 慢路径同步阻断 ulong 枚举的大值溢出行为
            if (t.IsEnum)
            {
                object rawValue = Type.GetTypeCode(t) == TypeCode.UInt64 ? (ulong)value : (long)value;
                return Enum.ToObject(t, rawValue);
            }

            return SerializationHelper.Deserialize(t, (ReadOnlyMemory<byte>)value);
        }

        #endregion
    }
}