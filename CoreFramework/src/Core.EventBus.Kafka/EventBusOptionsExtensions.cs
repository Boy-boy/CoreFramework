using System;
using System.Linq;
using Core.EventBus.Integration;
using Core.EventBus.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Kafka
{
    /// <summary>
    /// Kafka broker 的 <see cref="IEventBusOptionsExtensions"/> 实现:绑定
    /// <see cref="EventBusKafkaOptions"/>,桥接 connection 到 <c>CoreKafkaModule</c>,
    /// 并注册 publisher / subscriber / <see cref="IOutboxRawSender"/>。
    /// </summary>
    public class EventBusOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusKafkaOptions> _options;
        private readonly IConfiguration _configuration;

        public EventBusOptionsExtensions(Action<EventBusKafkaOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public EventBusOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void AddServices(IServiceCollection services)
        {
            // 同一进程仅允许一个 integration broker;检测到其他实现则抛错
            GuardSingleIntegrationBroker(services);

            var options = new EventBusKafkaOptions();
            if (_options != null)
            {
                services.Configure(_options);
                _options.Invoke(options);
            }
            else if (_configuration != null)
            {
                services.Configure<EventBusKafkaOptions>(_configuration);
                // 配置节点缺失/为空时 Get<T>() 返回 null,沿用默认实例
                var fromConfig = _configuration.Get<EventBusKafkaOptions>();
                if (fromConfig != null) options = fromConfig;
            }

            services.AddKafka(kafkaOptions =>
            {
                kafkaOptions.Connection = options.Connection;
                // 桥接到底层 consumer 的 Seek-retry 间隔
                kafkaOptions.FailureBackoff = options.FailureBackoff;
                // poison message 触顶 commit-skip 的最大重试次数
                kafkaOptions.MaxConsecutiveFailures = options.MaxConsecutiveFailures;
            });

            // publisher / IOutboxRawSender 用 Scoped:与 UoW 所在 scope 对齐,避免 outbox 写入后 DbContext 提前释放
            // subscriber 保留 Singleton:管理 broker 长连接
            services.TryAddScoped<IIntegrationPublisher, KafkaMessagePublisher>();
            services.TryAddSingleton<IIntegrationSubscriber, KafkaMessageSubscriber>();
            services.TryAddScoped(sp =>
                (IOutboxRawSender)sp.GetRequiredService<IIntegrationPublisher>());
            services.AddIntegrationCore();
        }

        /// <summary>已注册非自身实现 → 抛错;同 broker 重复调用幂等放行。工厂 / 实例注册也拒绝:IOutboxRawSender 会基于 IIntegrationPublisher 强转,自定义 publisher 不实现 IOutboxRawSender 将在运行时抛 InvalidCastException;需要自定义请同时显式注册 IOutboxRawSender 并跳过 AddRabbitMq/AddKafka。</summary>
        private static void GuardSingleIntegrationBroker(IServiceCollection services)
        {
            var existing = services.FirstOrDefault(s => s.ServiceType == typeof(IIntegrationPublisher));
            if (existing == null) return;
            if (existing.ImplementationType == typeof(KafkaMessagePublisher)) return;
            var label = existing.ImplementationType?.Name
                ?? (existing.ImplementationFactory != null ? "<factory>" : "<instance>");
            throw new InvalidOperationException(
                $"已注册 IIntegrationPublisher = {label};EventBus 同一时刻仅支持一个 integration broker,请只调用 AddRabbitMq / AddKafka 之一。自定义实现需自行注册 IIntegrationPublisher + IOutboxRawSender 并跳过 AddRabbitMq/AddKafka。");
        }
    }
}
