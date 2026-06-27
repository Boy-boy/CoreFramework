using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    /// <summary>
    /// 订阅项注册表抽象:维护"当前订阅了哪些 (messageType, handlerType) 对"的全局视图。
    /// </summary>
    /// <remarks>
    /// Local / Integration 各自有独立实例,避免本地事件订阅把集成端的注册表搞乱。
    /// </remarks>
    public interface IMessageHandlerManager
    {
        /// <summary>某 message type 的最后一个 handler 被移除时触发;集成端订阅器借此解绑路由 / 关闭 consumer。</summary>
        event EventHandler<Type> OnEventRemoved;

        /// <summary>当前已注册 wrapper 的不可变快照;读取者可安全枚举,不受并发写入影响。</summary>
        IReadOnlyList<IMessageHandlerWrapper> MessageHandlerWrappers { get; }

        /// <summary>注册一个 handler;同一 (messageType, handlerType) 重复注册将抛 <see cref="ArgumentException"/>。</summary>
        void AddHandler(Type messageType, Type handlerType);

        /// <summary>移除一个 handler;未注册时 no-op;移除后该 messageType 已无 handler 时触发 <see cref="OnEventRemoved"/>。</summary>
        void RemoveHandler(Type messageType, Type handlerType);
    }
}
