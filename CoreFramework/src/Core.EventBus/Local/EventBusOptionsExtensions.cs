using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Local
{
    /// <summary>本地事件总线的 options 扩展;publisher / subscriber / handler manager 均为 Singleton。</summary>
    /// <remarks>
    /// Singleton 选择:handler manager 需全局共享订阅表;publisher 无状态;subscriber 仅启动期用。
    /// </remarks>
    public class EventBusOptionsExtensions : IEventBusOptionsExtensions
    {
        /// <inheritdoc />
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<ILocalPublisher, LocalMessagePublisher>();
            services.TryAddSingleton<ILocalSubscriber, LocalMessageSubscriber>();
            services.TryAddSingleton<ILocalMessageHandlerManager, LocalMessageHandlerManager>();
        }
    }
}
