using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.SqlServer
{
    public class EventBusSqlServerOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusSqlServerOptions> _options;

        public EventBusSqlServerOptionsExtensions(Action<EventBusSqlServerOptions> options)
        {
            _options = options ?? throw new AggregateException(nameof(options));
        }
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<IStorage, SqlServerStorage>();
            services.TryAddTransient<ITransaction, SqlServerTransaction>();
            if (_options != null)
                services.Configure(_options);
        }
    }
}
