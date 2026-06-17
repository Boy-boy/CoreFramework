using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Integration
{
    /// <summary>
    /// 集成事件公共基础设施的 DI 注册扩展。
    /// 各 broker 模块（如 RabbitMQ）在自己的扩展里调用本方法注册 <see cref="IIntegrationMessageHandlerManager"/>，
    /// 避免每个 broker 都重复注册。
    /// </summary>
    public static class IntegrationServiceCollectionExtensions
    {
        /// <summary>
        /// 注册集成事件 handler 注册表（Singleton）。多次调用是安全的（TryAdd 语义）。
        /// </summary>
        public static IServiceCollection AddIntegrationCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IIntegrationMessageHandlerManager, IntegrationMessageHandlerManager>();
            return services;
        }
    }
}
