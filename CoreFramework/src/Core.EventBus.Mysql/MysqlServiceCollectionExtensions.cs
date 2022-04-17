using Microsoft.Extensions.DependencyInjection;
using System;
using Core.EventBus.Storage;
using Core.EventBus.Transaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Mysql
{
    public static class MysqlServiceCollectionExtensions
    {
        public static EventBusBuilder AddMysql(this EventBusBuilder builder,
            Action<EventBusMysqlOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            builder.Service.Configure(options);
            builder.Service.AddCore();
            return builder;
        }

        public static EventBusBuilder AddMysql(this EventBusBuilder builder,
            IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            builder.Service.Configure<EventBusMysqlOptions>(configuration);
            builder.Service.AddCore();
            return builder;
        }

        public static EventBusOptions AddMysql(this EventBusOptions options, Action<EventBusMysqlOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));
            options.AddExtensions(new EventBusMysqlOptionsExtensions(actionOptions));
            return options;
        }

        public static EventBusOptions AddMysql(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            options.AddExtensions(new EventBusMysqlOptionsExtensions(configuration));
            return options;
        }

        private static IServiceCollection AddCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IStorage, MysqlStorage>();
            services.TryAddTransient<ITransaction, MysqlTransaction>();
            return services;
        }
    }
}
