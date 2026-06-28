using System;
using System.Linq;

namespace Core.EventBus.Inbox
{
    /// <summary>
    /// 为 handler 显式指定 inbox 去重的 consumer group 标识。
    /// </summary>
    /// <remarks>
    /// <para><b>为什么需要</b></para>
    /// <para>
    /// inbox 表主键是 <c>(MessageId, ConsumerGroup)</c>。默认 ConsumerGroup 取
    /// <c>handler.GetType().FullName</c>,handler 重命名 / 换 namespace / 移动文件夹后,
    /// 上线第一批旧消息会因为 inbox 找不到旧记录而被<b>全部重处理一遍</b>。
    /// </para>
    /// <para>
    /// 关键业务的 handler 建议显式声明,把 inbox 标识与 CLR 类型解耦:
    /// </para>
    /// <code>
    /// [InboxConsumerGroup("order-charge-handler-v1")]
    /// public class ChargeCustomerHandler : IMessageHandler&lt;OrderCreatedEvent&gt; { ... }
    /// </code>
    /// <para>
    /// 之后无论 handler 类名/命名空间怎么改,inbox 去重都按显式标识来。
    /// </para>
    /// <para><b>一个 handler 多个 IMessageHandler&lt;T&gt; 的情形</b></para>
    /// <para>
    /// 类级特性对该 handler 处理的<b>所有</b>消息共享同一 consumer group。如需按消息类型分桶,改成
    /// 多个独立 handler 类(各自标自己的特性)是最稳妥的写法。
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class InboxConsumerGroupAttribute : Attribute
    {
        /// <summary>显式 consumer group 标识;与 handler CLR 类型解耦。</summary>
        public string Name { get; }

        public InboxConsumerGroupAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    $"{nameof(name)} can not be null, empty or white space!", nameof(name));
            }
            Name = name;
        }

        /// <summary>取 handler 类型上的特性值;未标注时回退到 <c>handler.FullName</c>(再回退到 <c>handler.Name</c>)。</summary>
        public static string GetGroupOrDefault(Type handlerType)
        {
            if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));
            var declared = handlerType
                .GetCustomAttributes(true)
                .OfType<InboxConsumerGroupAttribute>()
                .FirstOrDefault()
                ?.Name;
            if (!string.IsNullOrEmpty(declared)) return declared;
            return handlerType.FullName ?? handlerType.Name;
        }
    }
}
