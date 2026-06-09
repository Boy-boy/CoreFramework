using MessagePack;
using MessagePack.Resolvers;

namespace Core.Redis
{
    public static class SerializationHelper
    {
        private static readonly IFormatterResolver Resolver = CompositeResolver.Create(
            NativeDateTimeResolver.Instance,
            ContractlessStandardResolver.Instance
        );

        public static readonly MessagePackSerializerOptions SerializerOptions = MessagePackSerializerOptions.Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithResolver(Resolver);

        // ================= 泛型快路径：直接对接 MessagePack 核心，不走反射 =================

        public static byte[] Serialize<T>(T value)
        {
            try
            {
                // 值类型下，JIT 在编译期会把 value is null 直接裁掉，不影响性能
                return value is null
                    ? null
                    : MessagePackSerializer.Serialize<T>(value, SerializerOptions); // 👈 补上 <T> 开启快路径
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to serialize object.", ex);
            }
        }

        public static T Deserialize<T>(byte[] value)
        {
            if (value == null || value.Length == 0)
                return default!; // 👈 值类型直接返回 0/false，引用类型返回 null，无反射

            try
            {
                return MessagePackSerializer.Deserialize<T>(value, SerializerOptions); // 👈 直通泛型快路径
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize object.", ex);
            }
        }

        public static T Deserialize<T>(ReadOnlyMemory<byte> value)
        {
            if (value.IsEmpty)
                return default!;

            try
            {
                return MessagePackSerializer.Deserialize<T>(value, SerializerOptions); // 👈 直通泛型快路径
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize object.", ex);
            }
        }

        // ================= 非泛型慢路径：留给运行时未知类型（如 Nullable）兜底 =================

        public static object Deserialize(Type targetType, byte[] value)
        {
            try
            {
                if (value == null || value.Length == 0)
                    return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

                return MessagePackSerializer.Deserialize(targetType, value, SerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize object.", ex);
            }
        }

        public static object Deserialize(Type targetType, ReadOnlyMemory<byte> value)
        {
            try
            {
                if (value.IsEmpty)
                    return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

                return MessagePackSerializer.Deserialize(targetType, value, SerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize object.", ex);
            }
        }
    }
}