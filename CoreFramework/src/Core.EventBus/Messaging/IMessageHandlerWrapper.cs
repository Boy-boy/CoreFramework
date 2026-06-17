using System;

namespace Core.EventBus
{
    /// <summary>
    /// 订阅项的元数据载体。只承载"是哪种消息、由哪个 handler 类型处理、优先级是多少"等
    /// 描述信息，<b>不</b>缓存 handler 实例 —— handler 必须由 <see cref="IMessageHandlerInvoker"/>
    /// 在调用时基于 DI scope 重新 resolve，避免 scope 泄漏 / DbContext 错位。
    /// </summary>
    public interface IMessageHandlerWrapper
    {
        /// <summary>
        /// 消息名（典型为 <c>[MessageName]</c> 特性值或 CLR 全名）。
        /// broker 端用作 routing key，inbox 表用作 ConsumerGroup 检索辅助。
        /// </summary>
        string MessageName { get; }

        /// <summary>消息 CLR 类型；订阅匹配的"主键"维度。</summary>
        Type MessageType { get; }

        /// <summary>handler CLR 类型；用于在 DI scope 内 resolve 实例。</summary>
        Type HandlerType { get; }

        /// <summary>
        /// 同一 message 下多个 handler 时的执行优先级，由 <see cref="MessageHandlerPriorityAttribute"/> 提供。
        /// 数值越大越先执行；默认 0。
        /// </summary>
        int HandlerPriority { get; }
    }
}
