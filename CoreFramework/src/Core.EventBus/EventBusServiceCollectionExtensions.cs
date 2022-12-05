using Core.EventBus;
using Core.EventBus.Transaction;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusServiceCollectionExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.TryAddSingleton<ITransactionAccessor, TransactionAccessor>();
            services.TryAddTransient<ITransaction, Transaction>();
            services.AddHostedService<EventBusBackgroundService>();
            services.Configure(configureOptions);

            var options = new EventBusOptions();
            configureOptions.Invoke(options);
            options.Configure(services);
            return services;
        }
    }
}
