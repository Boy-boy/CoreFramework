using Microsoft.Extensions.DependencyInjection;
using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.PostgreSql
{
    public static class PostgreSqlServiceCollectionExtensions
    {
        public static EventBusBuilder AddPostgreSql(this EventBusBuilder builder,
            Action<EventBusPostgreSqlOptions> options)
        {
            if (options == null)
                throw new AggregateException(nameof(options));
            builder.Service.Configure(options);
            builder.Service.AddCore();
            return builder;
        }

        public static EventBusBuilder AddPostgreSql(this EventBusBuilder builder,
            IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            builder.Service.Configure<EventBusPostgreSqlOptions>(configuration);
            builder.Service.AddCore();
            return builder;
        }


        public static EventBusOptions AddPostgreSql(this EventBusOptions options, Action<EventBusPostgreSqlOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));
            options.AddExtensions(new EventBusPostgreSqlOptionsExtensions(actionOptions));
            return options;
        }

        public static EventBusOptions AddPostgreSql(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            options.AddExtensions(new EventBusPostgreSqlOptionsExtensions(configuration));
            return options;
        }

        private static IServiceCollection AddCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IStorage, PostgreSqlStorage>();
            services.TryAddTransient<ITransaction, PostgreSqlTransaction>();
            return services;
        }
    }
}
