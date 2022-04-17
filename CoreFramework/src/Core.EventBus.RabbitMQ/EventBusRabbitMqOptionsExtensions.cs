using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.RabbitMQ
{
    public class EventBusRabbitMqOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusRabbitMqOptions> _options;
        private readonly IConfiguration _configuration;

        public EventBusRabbitMqOptionsExtensions(Action<EventBusRabbitMqOptions> options)
        {
            _options = options;
        }

        public EventBusRabbitMqOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                new EventBusBuilder(services).AddRabbitMq(_options);
            }
            else if (_configuration != null)
            {
                new EventBusBuilder(services).AddRabbitMq(_configuration);
            }
        }
    }
}
