using System;
using Core.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Kafka 基础设施 DI 注册扩展。镜像 <c>RabbitMqServiceCollectionExtensions</c>。
    /// </summary>
    public static class KafkaServiceCollectionExtensions
    {
        public static IServiceCollection AddKafka(this IServiceCollection services, Action<KafkaOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.TryAddSingleton<IKafkaPersistentProducer, DefaultKafkaPersistentProducer>();
            services.TryAddSingleton<IKafkaMessageConsumerManager, DefaultKafkaMessageConsumerManager>();
            services.Configure(configureOptions);
            return services;
        }

        public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.TryAddSingleton<IKafkaPersistentProducer, DefaultKafkaPersistentProducer>();
            services.TryAddSingleton<IKafkaMessageConsumerManager, DefaultKafkaMessageConsumerManager>();
            services.Configure<KafkaOptions>(configuration);
            return services;
        }
    }
}
