using System;
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

            // publisher 注册为 Scoped:它在 PublishAsync 里用注入的 IServiceProvider 解析 IOutboxStorage,
            // 必须是当前请求 scope 的 SP,才能让 storage 创建/复用的 DbContext 由 UoW 所在 scope 持有,
            // 避免临时 scope dispose 导致外层 UoW 持有已释放的 DbContext。
            // subscriber 仍是 Singleton:管理 broker 长连接 / consumer。
            // IOutboxRawSender 同步降为 Scoped:它委托给 IIntegrationPublisher,从根容器解析 Scoped 会触发 scope-validation。
            // OutboxDispatcher 调用时本就在自建的 scope 内 → 兼容。
            services.TryAddScoped<IIntegrationPublisher, KafkaMessagePublisher>();
            services.TryAddSingleton<IIntegrationSubscriber, KafkaMessageSubscriber>();
            services.TryAddScoped(sp =>
                (IOutboxRawSender)sp.GetRequiredService<IIntegrationPublisher>());
            services.AddIntegrationCore();
        }
    }
}
