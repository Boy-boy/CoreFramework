using Core.Alert;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlertServiceCollectionExtensions
    {
        /// <summary>
        /// 注册告警升级核心能力，默认使用内存存储。
        /// </summary>
        public static IServiceCollection AddAlertEscalation(this IServiceCollection services)
            => services.AddAlertEscalation(_ => { });

        /// <summary>
        /// 注册告警升级核心能力，并允许调用方附加存储扩展。
        /// 若未显式覆盖 <see cref="IAlertStorageProvider"/>，则回退到内存实现。
        /// </summary>
        public static IServiceCollection AddAlertEscalation(
            this IServiceCollection services,
            Action<AlertOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.Configure(configureOptions);
            services.TryAddSingleton<AlertEscalationManager>();
            services.TryAddSingleton<IAlertStorageProvider, InMemoryAlertStorageProvider>();

            var options = new AlertOptions();
            configureOptions.Invoke(options);
            options.Configure(services);
            return services;
        }
    }
}
