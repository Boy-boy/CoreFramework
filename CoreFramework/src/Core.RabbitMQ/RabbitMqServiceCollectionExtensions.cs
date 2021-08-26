using Core.RabbitMQ;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMqServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, Action<RabbitMqOptions> configureOptions = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.TryAddSingleton<IRabbitMqPersistentConnection, DefaultRabbitMqPersistentConnection>();
            services.TryAddSingleton<IRabbitMqMessageConsumerFactory, DefaultRabbitMqMessageConsumerFactory>();
            if (configureOptions != null)
                services.Configure(configureOptions);
            return services;
        }
    }
}
