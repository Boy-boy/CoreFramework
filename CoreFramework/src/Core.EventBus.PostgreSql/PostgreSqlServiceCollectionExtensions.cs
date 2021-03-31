using Microsoft.Extensions.DependencyInjection;
using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.PostgreSql
{
    public static class PostgreSqlServiceCollectionExtensions
    {
        public static EventBusBuilder AddPostgreSql(this EventBusBuilder builder,
            Action<EventBusPostgreSqlOptions> options = null)
        {
            builder.Service.TryAddSingleton<IStorage, PostgreSqlStorage>();
            builder.Service.TryAddTransient<ITransaction, PostgreSqlTransaction>();
            if (options != null)
                builder.Service.Configure(options);
            return builder;
        }
    }
}
