using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Local
{
    /// <summary>本地事件总线的 options 扩展;按角色给出不同生命周期。</summary>
    /// <remarks>
    /// <para>生命周期选择:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="ILocalMessageHandlerManager"/>:<b>Singleton</b> — 需全局共享订阅表。</description></item>
    ///   <item><description><see cref="ILocalSubscriber"/>:<b>Singleton</b> — 仅启动期用,无状态。</description></item>
    ///   <item><description><see cref="ILocalPublisher"/> / <see cref="ILocalMessageHandlerInvoker"/>:<b>Scoped</b> —
    ///   必须能拿到调用方 scope 才能让 handler 复用当前 DbContext/UoW。详见 <see cref="ILocalMessageHandlerInvoker"/>
    ///   关于"为什么不能在外层 UoW 内开新 scope"的论述。</description></item>
    /// </list>
    /// </remarks>
    public class EventBusOptionsExtensions : IEventBusOptionsExtensions
    {
        /// <inheritdoc />
        public void AddServices(IServiceCollection services)
        {
            services.TryAddScoped<ILocalPublisher, LocalMessagePublisher>();
            services.TryAddScoped<ILocalMessageHandlerInvoker, LocalMessageHandlerInvoker>();
            services.TryAddSingleton<ILocalSubscriber, LocalMessageSubscriber>();
            services.TryAddSingleton<ILocalMessageHandlerManager, LocalMessageHandlerManager>();
        }
    }
}
