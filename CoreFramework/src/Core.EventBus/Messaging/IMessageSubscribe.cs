using System.Reflection;

namespace Core.EventBus
{
    /// <summary>
    /// 订阅器统一契约。由 <see cref="EventBusBackgroundService"/> 在启动时调用
    /// <see cref="Initialize"/> 把 (messageType, handlerType) 对推到具体实现（local / broker）。
    /// </summary>
    public interface IMessageSubscribe
    {
        /// <summary>
        /// 扫描入参 <paramref name="assemblies"/>，反射出所有 <see cref="IMessageHandler{TMessage}"/>
        /// 实现并完成订阅。<see cref="EventBusBackgroundService"/> 启动时调用一次。
        /// </summary>
        void Initialize(params Assembly[] assemblies);

        /// <summary>
        /// 单条订阅入口；运行时通过代码动态添加订阅时调用。
        /// </summary>
        void Subscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>;

        /// <summary>
        /// 取消订阅。所有 handler 都被移除后，broker 实现通常会解绑 routing key / 关闭 consumer。
        /// </summary>
        void UnSubscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>;
    }
}
