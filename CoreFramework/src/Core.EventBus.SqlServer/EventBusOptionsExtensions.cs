using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.SqlServer
{
    public class EventBusOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusSqlServerOptions> _options;
        private readonly IConfiguration _configuration;

        public EventBusOptionsExtensions(Action<EventBusSqlServerOptions> options)
        {
            _options = options;
        }

        public EventBusOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                services.Configure(_options);
            }
            else if (_configuration != null)
            {
                services.Configure<EventBusSqlServerOptions>(_configuration);
            }

            AddCore(services);
        }

        private IServiceCollection AddCore(IServiceCollection services)
        {
            services.TryAddSingleton<IStorage, SqlServerStorage>();
            services.TryAddTransient<ITransaction, SqlServerTransaction>();
            services.TryAddSingleton<StorageMarkerService>();
            return services;
        }
    }
}
