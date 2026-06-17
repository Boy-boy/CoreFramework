using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地事件总线的 <see cref="IEventBusOptionsExtensions"/> 实现：
    /// 把 publisher / subscribe / handler manager 注册为 Singleton。
    /// </summary>
    /// <remarks>
    /// 三者均为 Singleton：
    /// <list type="bullet">
    ///   <item><description>handler manager 需要全局共享订阅表（其它 scope 才能看到对方注册）</description></item>
    ///   <item><description>publisher 是无状态包装层，按 Singleton 避免每次注入再实例化</description></item>
    ///   <item><description>subscribe 只在启动期使用，Singleton 即可</description></item>
    /// </list>
    /// </remarks>
    public class EventBusOptionsExtensions : IEventBusOptionsExtensions
    {
        /// <inheritdoc />
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<ILocalMessagePublisher, LocalMessagePublisher>();
            services.TryAddSingleton<ILocalMessageSubscribe, LocalMessageSubscribe>();
            services.TryAddSingleton<ILocalMessageHandlerManager, LocalMessageHandlerManager>();
        }
    }
}
