using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    /// <summary>
    /// 订阅项注册表抽象：维护"当前订阅了哪些 (messageType, handlerType) 对"的全局视图。
    /// publisher 在分发消息时从这里取得 wrapper 列表；subscribe 在订阅/取消订阅时改写它。
    /// </summary>
    /// <remarks>
    /// Local / Integration 各自有一份独立实例（<see cref="Local.ILocalMessageHandlerManager"/> /
    /// <see cref="Integration.IIntegrationMessageHandlerManager"/>），避免本地事件订阅
    /// 把 broker 端的注册表搞乱。
    /// </remarks>
    public interface IMessageHandlerManager
    {
        /// <summary>
        /// 当某个 message type 的最后一个 handler 被 <see cref="RemoveHandler"/> 移除时触发。
        /// broker 侧订阅器借此解绑 routing key / 关闭 consumer。
        /// </summary>
        event EventHandler<Type> OnEventRemoved;

        /// <summary>
        /// 当前已注册 wrapper 的不可变快照。读取者可以安全枚举，不会被并发的
        /// <see cref="AddHandler"/> / <see cref="RemoveHandler"/> 影响。
        /// </summary>
        IReadOnlyList<IMessageHandlerWrapper> MessageHandlerWrappers { get; }

        /// <summary>
        /// 注册一个 handler。同一 (messageType, handlerType) 重复注册将抛 <see cref="ArgumentException"/>。
        /// </summary>
        void AddHandler(Type messageType, Type handlerType);

        /// <summary>
        /// 移除一个 handler。未注册时是 no-op；
        /// 移除后该 messageType 已无任何 handler 时触发 <see cref="OnEventRemoved"/>。
        /// </summary>
        void RemoveHandler(Type messageType, Type handlerType);
    }
}
