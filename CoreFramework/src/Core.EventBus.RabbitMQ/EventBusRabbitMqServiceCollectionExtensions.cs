using Core.EventBus.RabbitMQ;
using Core.EventBus;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusRabbitMqServiceCollectionExtensions
    {
        public static EventBusBuilder AddRabbitMq(this EventBusBuilder builder, Action<EventBusRabbitMqOptions> optionAction)
        {
            optionAction = optionAction ?? throw new AggregateException(nameof(optionAction));

            var option = new EventBusRabbitMqOptions();
            optionAction.Invoke(option);
            builder.Service.AddRabbitMq(option.RabbitMqOptions);

            builder.Service.TryAddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
            builder.Service.TryAddSingleton<IMessageSubscribe, RabbitMqMessageSubscribe>();
            builder.Service.Configure(optionAction);
            return builder;
        }

        public static EventBusOptions AddRabbitMq(this EventBusOptions options, Action<EventBusRabbitMqOptions> actionOptions)
        {
            options.AddExtensions(new EventBusRabbitMqOptionsExtensions(actionOptions));
            return options;
        }
    }
}
