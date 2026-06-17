using System;
using System.Linq;

namespace Core.EventBus
{
    /// <summary>
    /// 为消息类型显式指定"对外名字"。
    /// </summary>
    /// <remarks>
    /// <para><b>用途</b></para>
    /// <list type="bullet">
    ///   <item><description>broker routing key（如 RabbitMQ exchange.binding）</description></item>
    ///   <item><description>跨语言 / 跨服务的合约对齐 —— CLR 全名变更时仍能稳定路由</description></item>
    /// </list>
    /// <para>未标注时回退到 <see cref="Type.FullName"/>。</para>
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

        /// <summary>
        /// 取消息类型上的 <see cref="MessageNameAttribute"/> 值；未标注时返回 <see cref="Type.FullName"/>。
        /// </summary>
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
