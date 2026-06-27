using System;
using Core.EventBus.Integration;
using Core.EventBus.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Kafka
{
    /// <summary>
    /// Kafka broker 的 <see cref="IEventBusOptionsExtensions"/> 实现：
    /// 仅负责把 <see cref="EventBusKafkaOptions"/> 绑到 IOptions，并把 connection 配置桥接到底层 <c>CoreKafkaModule</c>。
    /// publisher / subscribe / IOutboxRawSender 等服务的注册由 <see cref="CoreEventBusKafkaModule"/> 完成，不在这里重复。
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
                // 配置节点缺失 / 为空时 Get<T>() 返回 null;沿用上面 new 出的默认实例
                var fromConfig = _configuration.Get<EventBusKafkaOptions>();
                if (fromConfig != null) options = fromConfig;
            }

            services.AddKafka(kafkaOptions =>
            {
                kafkaOptions.Connection = options.Connection;
                // 把 EventBus 维度的失败退避桥接到底层 consumer 的 Seek-retry 间隔
                kafkaOptions.FailureBackoff = options.FailureBackoff;
                // poison message 触顶 commit-skip 的最大重试次数
                kafkaOptions.MaxConsecutiveFailures = options.MaxConsecutiveFailures;
            });

            services.TryAddSingleton<IIntegrationPublisher, KafkaMessagePublisher>();
            services.TryAddSingleton<IIntegrationSubscriber, KafkaMessageSubscriber>();
            services.TryAddSingleton(sp =>
                (IOutboxRawSender)sp.GetRequiredService<IIntegrationPublisher>());
            services.AddIntegrationCore();
        }
    }
}
