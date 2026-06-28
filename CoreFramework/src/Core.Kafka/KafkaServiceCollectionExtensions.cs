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
        /// <summary>只注册 Kafka infrastructure singleton,不绑定任何 <see cref="KafkaOptions"/> 来源。</summary>
        /// <remarks>给上层模块用:它自己用 <c>AddOptions&lt;KafkaOptions&gt;().Configure&lt;IOptions&lt;XxxOptions&gt;&gt;(...)</c> 把配置联动过来,避免在 appsettings 里写两遍。</remarks>
        public static IServiceCollection AddKafka(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IKafkaPersistentProducer, DefaultKafkaPersistentProducer>();
            services.TryAddSingleton<IKafkaMessageConsumerManager, DefaultKafkaMessageConsumerManager>();
            return services;
        }

        /// <summary>用 Action 配置 Kafka,适合代码侧组装/测试。</summary>
        public static IServiceCollection AddKafka(this IServiceCollection services, Action<KafkaOptions> configureOptions)
        {
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.AddKafka();
            services.Configure(configureOptions);
            return services;
        }

        /// <summary>用 IConfiguration 节点(通常是 <c>Kafka</c>)配置 Kafka。</summary>
        public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.AddKafka();
            services.Configure<KafkaOptions>(configuration);
            return services;
        }
    }
}
