using Core.EventBus;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

            // 通过 IOptions 拿到聚合后的 options 再 Configure:
            // 用户回调只被 OptionsManager 调一次,且其他模块预先写入的 Configure 也能被应用,不再被默默丢失
            using var sp = services.BuildServiceProvider();
            var aggregated = sp.GetRequiredService<IOptions<EventBusOptions>>().Value;
            aggregated.Configure(services);
            return services;
        }
    }
}
