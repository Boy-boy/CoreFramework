using Core.EventBus;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using Core.EventBus.Transaction;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusServiceCollectionExtensions
    {
        public static EventBusBuilder AddEventBus(this IServiceCollection services, Action<EventBusOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.TryAddSingleton<IMessageHandlerManager, MessageHandlerManager>();
            services.TryAddSingleton<IMessageHandlerProvider, MessageHandlerProvider>();
            services.TryAddSingleton<ITransactionAccessor, TransactionAccessor>();
            services.AddHostedService<EventBusBackgroundService>();
            services.Configure(configureOptions);

            var options = new EventBusOptions();
            configureOptions.Invoke(options);
            options.Configure(services);
            return new EventBusBuilder(services);
        }
    }
}
