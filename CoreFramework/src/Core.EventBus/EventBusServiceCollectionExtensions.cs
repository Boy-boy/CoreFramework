using Core.EventBus;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 直接使用 <see cref="IServiceCollection"/> 注册 EventBus 的入口（非模块化场景）。
    /// </summary>
    /// <remarks>
    /// 模块化（<see cref="Core.Modularity"/>）环境下应该使用
    /// <see cref="Core.EventBus.CoreEventBusModule"/> + <see cref="Core.Modularity.Attribute.DependsOnAttribute"/> 注册；
    /// 直接 host / 测试场景才用本扩展。两者注册结果等价。
    /// </remarks>
    public static class EventBusServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 EventBus：基础设施 + <paramref name="configureOptions"/> 中声明的所有 extension。
        /// </summary>
        /// <param name="services">DI 容器。</param>
        /// <param name="configureOptions">配置回调，典型内容：<c>options.AddConsumers(...)</c>、<c>options.AddLocalMq()</c>、<c>options.AddRabbitMq(...)</c>。</param>
        public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.AddHostedService<EventBusBackgroundService>();
            services.TryAddSingleton<IMessageHandlerInvoker, DefaultMessageHandlerInvoker>();

            services.Configure(configureOptions);

            var options = new EventBusOptions();
            configureOptions.Invoke(options);
            options.Configure(services);
            return services;
        }
    }
}
