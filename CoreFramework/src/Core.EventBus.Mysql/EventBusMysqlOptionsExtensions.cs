using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;

namespace Core.EventBus.Mysql
{
    public class EventBusMysqlOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusMysqlOptions> _options;
        private readonly IConfiguration _configuration;

        public EventBusMysqlOptionsExtensions(Action<EventBusMysqlOptions> options)
        {
            _options = options;
        }
        public EventBusMysqlOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                new EventBusBuilder(services).AddMysql(_options);
            }
            else if (_configuration != null)
            {
                new EventBusBuilder(services).AddMysql(_configuration);
            }
        }
    }
}
