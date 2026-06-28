using System;
using Core.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Kafka 基础设施 DI 注册扩展;注册 <see cref="IKafkaPersistentProducer"/> /
    /// <see cref="IKafkaMessageConsumerManager"/> 与 <see cref="KafkaOptions"/> 绑定。
    /// </summary>
    public static class KafkaServiceCollectionExtensions
    {
        /// <summary>用 Action 配置 Kafka,适合代码侧组装/测试。</summary>
        public static IServiceCollection AddKafka(this IServiceCollection services, Action<KafkaOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.TryAddSingleton<IKafkaPersistentProducer, DefaultKafkaPersistentProducer>();
            services.TryAddSingleton<IKafkaMessageConsumerManager, DefaultKafkaMessageConsumerManager>();
            services.Configure(configureOptions);
            return services;
        }

        /// <summary>用 IConfiguration 节点(通常是 <c>Kafka</c>)配置 Kafka。</summary>
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
