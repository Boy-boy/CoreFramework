using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.RabbitMQ
{
    public class EventBusRabbitMqOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusRabbitMqOptions> _options;

        public EventBusRabbitMqOptionsExtensions(Action<EventBusRabbitMqOptions> options)
        {
            _options = options ?? throw new AggregateException(nameof(options));
        }
        public void AddServices(IServiceCollection services)
        {
            var option = new EventBusRabbitMqOptions();
            _options.Invoke(option);
            services.AddRabbitMq(option.RabbitMqOptions);

            services.TryAddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
            services.TryAddSingleton<IMessageSubscribe, RabbitMqMessageSubscribe>();
            services.Configure(_options);
        }
    }
}
