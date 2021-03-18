using Core.EventBus.RabbitMQ;
using Core.EventBus;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusRabbitMqServiceCollectionExtensions
    {
        public static EventBusBuilder AddRabbitMq(this EventBusBuilder builder, Action<EventBusRabbitMqOptions> options = null)
        {
            builder.Service.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
            builder.Service.AddSingleton<IMessageSubscribe, RabbitMqMessageSubscribe>();
            if (options == null) return builder;
            builder.Service.Configure(options);
            return builder;
        }
    }
}
