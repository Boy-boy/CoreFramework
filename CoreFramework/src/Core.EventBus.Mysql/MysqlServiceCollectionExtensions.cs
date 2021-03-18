using Microsoft.Extensions.DependencyInjection;
using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Mysql
{
    public static class MysqlServiceCollectionExtensions
    {
        public static EventBusBuilder AddMysql(this EventBusBuilder builder,
            Action<EventBusMysqlOptions> options = null)
        {
            builder.Service.TryAddSingleton<IStorage, MysqlStorage>();
            builder.Service.TryAddTransient<ITransaction, MysqlTransaction>();
            if (options != null)
                builder.Service.Configure(options);
            return builder;
        }
    }
}
