using System;

namespace Core.EventBus
{
    /// <summary>
    /// 订阅项的元数据载体;只承载描述信息,不缓存 handler 实例(实例必须由 <see cref="IMessageHandlerInvoker"/> 按 scope 重新 resolve)。
    /// </summary>
    public interface IMessageHandlerWrapper
    {
        /// <summary>消息名:broker 端作 routing key,inbox 表作 ConsumerGroup 检索辅助。</summary>
        string MessageName { get; }

        /// <summary>消息 CLR 类型;订阅匹配的主键维度。</summary>
        Type MessageType { get; }

        /// <summary>handler CLR 类型;用于在 DI scope 内 resolve 实例。</summary>
        Type HandlerType { get; }

        /// <summary>多 handler 时的执行优先级,数值越大越先执行;默认 0。</summary>
        int HandlerPriority { get; }
    }
}
