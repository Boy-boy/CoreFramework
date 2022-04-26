using Core.EventBus.RabbitMQ;
using Core.EventBus;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusRabbitMqServiceCollectionExtensions
    {
        public static EventBusBuilder AddRabbitMq(this EventBusBuilder builder, Action<EventBusRabbitMqOptions> optionAction)
        {
            if (optionAction == null)
                throw new AggregateException(nameof(optionAction));
            builder.Service.Configure(optionAction);

            var options = new EventBusRabbitMqOptions();
            optionAction.Invoke(options);
            builder.Service.AddCore(options);
            return builder;
        }

        public static EventBusBuilder AddRabbitMq(this EventBusBuilder builder, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            builder.Service.Configure<EventBusRabbitMqOptions>(configuration);

            var options = configuration.Get<EventBusRabbitMqOptions>();
            builder.Service.AddCore(options);
            return builder;
        }

        public static EventBusOptions AddRabbitMq(this EventBusOptions options, Action<EventBusRabbitMqOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));

            options.AddExtensions(new EventBusRabbitMqOptionsExtensions(actionOptions));
            return options;
        }

        public static EventBusOptions AddRabbitMq(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            options.AddExtensions(new EventBusRabbitMqOptionsExtensions(configuration));
            return options;
        }

        private static IServiceCollection AddCore(this IServiceCollection services, EventBusRabbitMqOptions options)
        {
            services.AddRabbitMq(rabbitMqOptions =>
            {
                rabbitMqOptions.Connection = options.Connection;
            });
            services.TryAddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
            services.TryAddSingleton<IMessageSubscribe, RabbitMqMessageSubscribe>();
            return services;
        }
    }
}
