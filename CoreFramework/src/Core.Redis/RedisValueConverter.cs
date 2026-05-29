using StackExchange.Redis;
using System.Globalization;
using System.Runtime.Serialization;

namespace Core.Redis
{
    public static class RedisValueConverter
    {
        private static readonly Dictionary<Type, Func<object, RedisValue>> ConvertToRedisValueMap = new()
        {
            { typeof(string),             v => (string)v },
            { typeof(int),                v => (int)v },
            { typeof(uint),               v => (uint)v },
            { typeof(double),             v => (double)v },
            { typeof(byte[]),             v => (byte[])v },
            { typeof(bool),               v => (bool)v },
            { typeof(long),               v => (long)v },
            { typeof(ulong),              v => (ulong)v },
            { typeof(float),              v => (float)v },
            { typeof(Guid),               v => ((Guid)v).ToString("D", CultureInfo.InvariantCulture) },
            { typeof(DateTime),           v => ((DateTime)v).ToString("o", CultureInfo.InvariantCulture) },
            { typeof(DateTimeOffset),     v => ((DateTimeOffset)v).ToString("o", CultureInfo.InvariantCulture) },
            { typeof(ReadOnlyMemory<byte>), v => (ReadOnlyMemory<byte>)v },
            { typeof(Memory<byte>),       v => (Memory<byte>)v },
            { typeof(RedisValue),         v => (RedisValue)v }
        };

        private static readonly Dictionary<Type, Func<RedisValue, object>> ConvertFromRedisValueMap = new()
        {
            { typeof(string),             v => (string)v },
            { typeof(int),                v => (int)v },
            { typeof(uint),               v => uint.Parse((string)v ?? string.Empty, CultureInfo.InvariantCulture) },
            { typeof(double),             v => (double)v },
            { typeof(byte[]),             v => (byte[])v },
            { typeof(bool),               v => (bool)v },
            { typeof(long),               v => (long)v },
            { typeof(ulong),              v => ulong.Parse((string)v ?? string.Empty, CultureInfo.InvariantCulture) },
            { typeof(float),              v => (float)v },
            { typeof(Guid),               v => ParseGuid(v) },
            { typeof(DateTime),           v => DateTime.Parse((string)v ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) },
            { typeof(DateTimeOffset),     v => DateTimeOffset.Parse((string)v ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) },
            { typeof(ReadOnlyMemory<byte>), v => (ReadOnlyMemory<byte>)v },
            { typeof(Memory<byte>),       v => (ReadOnlyMemory<byte>)v },
            { typeof(RedisValue),         v => v }
        };

        public static RedisValue ToRedisValue(object value)
        {
            if (value == null)
                return RedisValue.Null;

            var type = value.GetType();
            if (ConvertToRedisValueMap.TryGetValue(type, out var converter))
                return converter(value);

            return SerializationHelper.Serialize(value);
        }

        public static T FromRedisValue<T>(RedisValue value)
        {
            return (T)FromRedisValue(typeof(T), value);
        }

        public static object FromRedisValue(Type targetType, RedisValue value)
        {
            ArgumentNullException.ThrowIfNull(targetType);

            try
            {
                if (value.IsNull)
                    return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

                if (targetType == typeof(object))
                    return value;

                if (ConvertFromRedisValueMap.TryGetValue(targetType, out var converter))
                {
                    try
                    {
                        return converter(value);
                    }
                    catch (Exception conversionEx)
                    {
                        throw new InvalidCastException(
                            $"Failed to convert RedisValue to {targetType.FullName}. Value: {value}",
                            conversionEx);
                    }
                }

                var bytes = (byte[])value;
                try
                {
                    return SerializationHelper.Deserialize(targetType, bytes);
                }
                catch (Exception deserializeEx)
                {
                    throw new SerializationException(
                        $"Failed to deserialize to {targetType.FullName}. Byte length: {bytes?.Length ?? 0}",
                        deserializeEx);
                }
            }
            catch (Exception ex) when (ex is not InvalidCastException && ex is not SerializationException)
            {
                throw new InvalidOperationException(
                    $"Unexpected error converting RedisValue to {targetType.FullName}. IsNull: {value.IsNull}",
                    ex);
            }
        }

        private static Guid ParseGuid(RedisValue value)
        {
            var s = (string)value;
            return string.IsNullOrEmpty(s) ? Guid.Empty : Guid.Parse(s);
        }
    }
}
