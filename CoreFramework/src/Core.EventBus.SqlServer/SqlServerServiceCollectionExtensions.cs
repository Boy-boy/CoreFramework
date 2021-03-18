using Microsoft.Extensions.DependencyInjection;
using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.SqlServer
{
    public static class SqlServerServiceCollectionExtensions
    {
        public static EventBusBuilder AddSqlServer(this EventBusBuilder builder,
            Action<EventBusSqlServerOptions> options = null)
        {
            builder.Service.TryAddSingleton<IStorage, SqlServerStorage>();
            builder.Service.TryAddTransient<ITransaction, SqlServerTransaction>();
            if (options != null)
                builder.Service.Configure(options);
            return builder;
        }
    }
}
