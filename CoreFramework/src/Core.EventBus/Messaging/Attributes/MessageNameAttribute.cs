using System;
using System.Linq;

namespace Core.EventBus
{
    /// <summary>
    /// 为消息类型显式指定对外名字;用于稳定的路由 key 与跨服务合约对齐。
    /// </summary>
    /// <remarks>
    /// 未标注时回退到 <see cref="Type.FullName"/>。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageNameAttribute : Attribute
    {
        /// <summary>消息名。</summary>
        public virtual string Name { get; }

        public MessageNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"{nameof(name)} can not be null, empty or white space!");
            }
            Name = name;
        }

        /// <summary>取消息类型上的特性值;未标注时返回 <see cref="Type.FullName"/>。</summary>
        public static string GetNameOrDefault(Type messageType)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }
            return messageType
                       .GetCustomAttributes(true)
                       .OfType<MessageNameAttribute>()
                       .FirstOrDefault()
                       ?.Name
                   ?? messageType.FullName;
        }
    }
}
