using Core.EventBus;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 直接使用 <see cref="IServiceCollection"/> 注册 EventBus 的入口(非模块化场景)。
    /// </summary>
    /// <remarks>
    /// 模块化环境应使用 <see cref="Core.EventBus.CoreEventBusModule"/>;直接 host / 测试场景才用本扩展。两者注册结果等价。
    /// </remarks>
    public static class EventBusServiceCollectionExtensions
    {
        /// <summary>注册 EventBus:基础设施 + <paramref name="configureOptions"/> 中声明的所有 extension。</summary>
        public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.AddHostedService<EventBusBackgroundService>();
            services.TryAddSingleton<IMessageHandlerInvoker, DefaultMessageHandlerInvoker>();

            services.Configure(configureOptions);

            // 不 BuildServiceProvider:直接走描述符聚合所有 IConfigureOptions<EventBusOptions> 即可。
            // 详见 EventBusOptionsExtensions.ResolveAndConfigureEventBus。
            services.ResolveAndConfigureEventBus();
            return services;
        }
    }
}
