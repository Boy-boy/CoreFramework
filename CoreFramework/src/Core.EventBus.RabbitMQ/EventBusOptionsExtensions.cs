using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.RabbitMQ
{
    public class EventBusOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusRabbitMqOptions> _options;
        private readonly IConfiguration _configuration;

        public EventBusOptionsExtensions(Action<EventBusRabbitMqOptions> options)
        {
            _options = options;
        }

        public EventBusOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddServices(IServiceCollection services)
        {
            var options = new EventBusRabbitMqOptions();
            if (_options != null)
            {
                services.Configure(_options);
                _options.Invoke(options);
            }
            else if (_configuration != null)
            {
                services.Configure<EventBusRabbitMqOptions>(_configuration);
                options = _configuration.Get<EventBusRabbitMqOptions>();

            }

            AddCore(services, options);
        }

        private IServiceCollection AddCore(IServiceCollection services, EventBusRabbitMqOptions options)
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
