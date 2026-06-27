using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus
{
    /// <summary>
    /// 订阅器统一契约;由 <see cref="EventBusBackgroundService"/> 在启动时调用 <see cref="InitializeAsync"/> 完成订阅。
    /// </summary>
    public interface IMessageSubscriber
    {
        /// <summary>扫描入参程序集,反射出所有 <see cref="IMessageHandler{TMessage}"/> 实现并完成订阅;启动时调用一次。</summary>
        Task InitializeAsync(Assembly[] assemblies, CancellationToken cancellationToken = default);

        /// <summary>单条订阅入口;运行时动态添加订阅时调用。</summary>
        Task SubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
            where T : class, IMessage
            where TH : IMessageHandler<T>;

        /// <summary>取消订阅;所有 handler 都被移除后,集成端实现通常会解绑路由 / 关闭 consumer。</summary>
        Task UnSubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
            where T : class, IMessage
            where TH : IMessageHandler<T>;
    }
}
