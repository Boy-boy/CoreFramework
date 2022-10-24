using Core.EventBus;
using Core.EventBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using System;
using EventBusOptionsExtensions = Core.EventBus.RabbitMQ.EventBusOptionsExtensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusRabbitMqServiceCollectionExtensions
    {
        public static EventBusOptions AddRabbitMq(this EventBusOptions options, Action<EventBusRabbitMqOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));

            options.AddExtensions(new EventBusOptionsExtensions(actionOptions));
            return options;
        }

        public static EventBusOptions AddRabbitMq(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            options.AddExtensions(new EventBusOptionsExtensions(configuration));
            return options;
        }
    }
}
