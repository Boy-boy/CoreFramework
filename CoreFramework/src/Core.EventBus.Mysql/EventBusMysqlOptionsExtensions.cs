using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Core.EventBus.Mysql
{
    public class EventBusMysqlOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusMysqlOptions> _options;

        public EventBusMysqlOptionsExtensions(Action<EventBusMysqlOptions> options)
        {
            _options = options ?? throw new AggregateException(nameof(options));
        }
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<IStorage, MysqlStorage>();
            services.TryAddTransient<ITransaction, MysqlTransaction>();
            if (_options != null)
                services.Configure(_options);
        }
    }
}
