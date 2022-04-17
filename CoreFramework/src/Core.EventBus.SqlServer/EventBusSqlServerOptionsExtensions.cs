using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.SqlServer
{
    public class EventBusSqlServerOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusSqlServerOptions> _options;
        private readonly IConfiguration _configuration;

        public EventBusSqlServerOptionsExtensions(Action<EventBusSqlServerOptions> options)
        {
            _options = options;
        }

        public EventBusSqlServerOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                new EventBusBuilder(services).AddSqlServer(_options);
            }
            else if (_configuration != null)
            {
                new EventBusBuilder(services).AddSqlServer(_configuration);
            }
        }
    }
}
