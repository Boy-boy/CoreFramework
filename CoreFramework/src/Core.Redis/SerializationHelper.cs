using MessagePack;
using MessagePack.Resolvers;

namespace Core.Redis
{
    public static class SerializationHelper
    {
        // 使用 NativeDateTimeResolver 来保留原始 DateTime.Kind
        private static readonly IFormatterResolver Resolver = CompositeResolver.Create(
            NativeDateTimeResolver.Instance,
            ContractlessStandardResolver.Instance
        );

        public static readonly MessagePackSerializerOptions SerializerOptions = MessagePack.MessagePackSerializerOptions.Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithResolver(Resolver);

        public static byte[] Serialize<T>(T value)
        {
            try
            {
                return value == null
                    ? null
                    : MessagePackSerializer.Serialize(value, SerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to serialize object.", ex);
            }
        }

        public static T Deserialize<T>(byte[] value)
        {
            return (T)Deserialize(typeof(T), value);
        }

        public static object Deserialize(Type targetType, byte[] value)
        {
            try
            {
                // 1. 处理 null 或空数据
                if (value == null || value.Length == 0)
                    return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

                // 使用 MessagePack 反序列化
                return MessagePackSerializer.Deserialize(targetType, value, SerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize object.", ex);
            }
        }

        /// <summary>
        /// 按精确长度收束的反序列化重载。
        /// 用于从 ArrayPool 租借的、可能比有效数据更大的缓冲区中读取，
        /// 避免把池化数组尾部的残留脏数据当成 MessagePack 内容解析。
        /// </summary>
        public static T Deserialize<T>(ReadOnlyMemory<byte> value)
        {
            return (T)Deserialize(typeof(T), value);
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
