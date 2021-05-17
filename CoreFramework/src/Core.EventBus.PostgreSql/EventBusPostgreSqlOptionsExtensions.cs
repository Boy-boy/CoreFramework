using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.PostgreSql
{
    public class EventBusPostgreSqlOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusPostgreSqlOptions> _options;

        public EventBusPostgreSqlOptionsExtensions(Action<EventBusPostgreSqlOptions> options)
        {
            _options = options ?? throw new AggregateException(nameof(options));
        }
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<IStorage, PostgreSqlStorage>();
            services.TryAddTransient<ITransaction, PostgreSqlTransaction>();
            if (_options != null)
                services.Configure(_options);
        }
    }
}
