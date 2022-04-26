using Core.RabbitMQ;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMqServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, Action<RabbitMqOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.TryAddSingleton<IRabbitMqPersistentConnection, DefaultRabbitMqPersistentConnection>();
            services.TryAddSingleton<IRabbitMqMessageConsumerManager, DefaultRabbitMqMessageConsumerManager>();
            services.Configure(configureOptions);
            return services;
        }

        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.TryAddSingleton<IRabbitMqPersistentConnection, DefaultRabbitMqPersistentConnection>();
            services.TryAddSingleton<IRabbitMqMessageConsumerManager, DefaultRabbitMqMessageConsumerManager>();
            services.Configure<RabbitMqOptions>(configuration);
            return services;
        }
    }
}
