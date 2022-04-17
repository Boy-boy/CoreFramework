using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.PostgreSql
{
    public class EventBusPostgreSqlOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusPostgreSqlOptions> _options;
        private readonly IConfiguration _configuration;

        public EventBusPostgreSqlOptionsExtensions(Action<EventBusPostgreSqlOptions> options)
        {
            _options = options;
        }
        public EventBusPostgreSqlOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                new EventBusBuilder(services).AddPostgreSql(_options);
            }
            else if (_configuration != null)
            {
                new EventBusBuilder(services).AddPostgreSql(_configuration);
            }
        }
    }
}
