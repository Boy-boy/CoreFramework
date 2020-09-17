using Core.EventBus.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.EventBus.RabbitMQ
{
    public static class EventBusRabbitMqCollectionExtensions
    {
        public static EventBusBuilder AddRabbitMq(this EventBusBuilder eventBusBuilder, Action<EventBusRabbitMqOptions> configure=null)
        {
            eventBusBuilder.Services.AddSingleton<IEventBus, EventBusRabbitMq>();
            if (configure == null) return eventBusBuilder;
            eventBusBuilder.Services.Configure(configure);
            return eventBusBuilder;
        }
    }
}
