using StackExchange.Redis;
using System.Runtime.Serialization;

namespace Core.Redis
{
    public class RedisValueConverter
    {
        private static readonly Dictionary<Type, Func<object, RedisValue>> ConvertToRedisValueMap = new Dictionary<Type, Func<object, RedisValue>>()
    {
        { typeof(string), v => (string)v },
        { typeof(int), v => (int)v },
        { typeof(uint), v => (uint)v },
        { typeof(double), v => (double)v },
        { typeof(byte[]), v => (byte[])v },
        { typeof(bool), v => (bool)v },
        { typeof(long), v => (long)v },
        { typeof(ulong), v => (ulong)v },
        { typeof(float), v => (float)v },
        { typeof(Guid), v => v.ToString() },
        { typeof(DateTime), v => ((DateTime)v).ToString("o") },
        { typeof(DateTimeOffset), v => ((DateTimeOffset)v).ToString("o") },
        { typeof(ReadOnlyMemory<byte>), v => (ReadOnlyMemory<byte>)v },
        { typeof(Memory<byte>), v => (Memory<byte>)v },
        { typeof(RedisValue), v => (RedisValue)v }
    };

        private static readonly Dictionary<Type, Func<RedisValue, object>> ConvertFromRedisValueMap = new Dictionary<Type, Func<RedisValue, object>>()
    {
        { typeof(string), v => (string)v },
        { typeof(int), v => (int)v },
        { typeof(uint), v => (uint)(int)v },
        { typeof(double), v => (double)v },
        { typeof(byte[]), v => (byte[])v },
        { typeof(bool), v => (bool)v },
        { typeof(long), v => (long)v },
        { typeof(ulong), v => (ulong)(long)v },
        { typeof(float), v => (float)v },
        { typeof(Guid), v => new Guid((string)v ?? string.Empty) },
        { typeof(DateTime), v => DateTime.Parse(v) },
        { typeof(DateTimeOffset), v => DateTimeOffset.Parse(v) },
        { typeof(ReadOnlyMemory<byte>), v => (ReadOnlyMemory<byte>)v },
        { typeof(Memory<byte>), v => (ReadOnlyMemory<byte>)v },
        { typeof(RedisValue), v => (RedisValue)v }
    };

        public static RedisValue ToRedisValue(object value)
        {
            if (value == null)
            {
                return RedisValue.Null;
            }

            var type = value.GetType();
            if (ConvertToRedisValueMap.TryGetValue(type, out var converter))
            {
                return converter(value);
            }

            return SerializationHelper.Serialize(value);
        }

        public static T FromRedisValue<T>(RedisValue value)
        {
            return (T)FromRedisValue(typeof(T), value);
        }

        public static object FromRedisValue(Type targetType, RedisValue value)
        {
            try
            {
                if (value.IsNull)
                {
                    // 处理值类型的默认值
                    return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                }

                // 处理动态对象请求
                if (targetType == typeof(object))
                {
                    return value;
                }

                // 尝试从预定义的转换器中获取
                if (ConvertFromRedisValueMap.TryGetValue(targetType, out var converter))
                {
                    try
                    {
                        return converter(value);
                    }
                    catch (Exception conversionEx)
                    {
                        throw new InvalidCastException(
                            $"Failed to convert RedisValue to {targetType.FullName}. " +
                            $"Value: {value.ToString()}", conversionEx);
                    }
                }

                // 反序列化处理
                try
                {
                    return SerializationHelper.Deserialize(targetType, (byte[])value);
                }
                catch (Exception deserializeEx)
                {
                    throw new SerializationException(
                        $"Failed to deserialize to {targetType.FullName}. " +
                        $"Byte length: {((byte[])value)?.Length ?? 0}", deserializeEx);
                }
            }
            catch (Exception ex)
            {
                // 如果是已经包装过的异常，直接抛出
                if (ex is InvalidCastException || ex is SerializationException)
                    throw;

                // 包装未处理的异常
                throw new InvalidOperationException(
                    $"Unexpected error converting RedisValue to {targetType?.FullName ?? "null"}. " +
                    $"Value type: {value.GetType()}, IsNull: {value.IsNull}", ex);
            }
        }
    }
}
