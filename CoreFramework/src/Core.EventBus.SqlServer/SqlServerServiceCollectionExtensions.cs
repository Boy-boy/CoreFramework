using Microsoft.Extensions.DependencyInjection;
using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.SqlServer
{
    public static class SqlServerServiceCollectionExtensions
    {
        public static EventBusBuilder AddSqlServer(this EventBusBuilder builder,
            Action<EventBusSqlServerOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            builder.Service.Configure(options);
            builder.Service.AddCore();
            return builder;
        }

        public static EventBusBuilder AddSqlServer(this EventBusBuilder builder,
            IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            builder.Service.Configure<EventBusSqlServerOptions>(configuration);
            builder.Service.AddCore();
            return builder;
        }

        public static EventBusOptions AddSqlServer(this EventBusOptions options, Action<EventBusSqlServerOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));
            options.AddExtensions(new EventBusSqlServerOptionsExtensions(actionOptions));
            return options;
        }

        public static EventBusOptions AddSqlServer(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            options.AddExtensions(new EventBusSqlServerOptionsExtensions(configuration));
            return options;
        }

        private static IServiceCollection AddCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IStorage, SqlServerStorage>();
            services.TryAddTransient<ITransaction, SqlServerTransaction>();
            return services;
        }
    }
}
