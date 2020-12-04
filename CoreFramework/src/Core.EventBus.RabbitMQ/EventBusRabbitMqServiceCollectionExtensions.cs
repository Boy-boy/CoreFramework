using Core.EventBus.Abstraction;
using Core.EventBus.RabbitMQ;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusRabbitMqServiceCollectionExtensions
    {
        public static IServiceCollection AddEventBusRabbitMq(this IServiceCollection services, Action<EventBusRabbitMqOptions> configure=null)
        {
            services.AddSingleton<IEventBus, EventBusRabbitMq>();
            if (configure == null) return services;
            services.Configure(configure);
            return services;
        }
    }
}
