using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Integration
{
    /// <summary>集成事件公共基础设施的 DI 注册;由各 broker 模块调用,避免重复注册。</summary>
    public static class IntegrationServiceCollectionExtensions
    {
        /// <summary>注册集成事件 handler 注册表(Singleton);TryAdd 语义,多次调用安全。</summary>
        public static IServiceCollection AddIntegrationCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IIntegrationMessageHandlerManager, IntegrationMessageHandlerManager>();
            return services;
        }
    }
}
