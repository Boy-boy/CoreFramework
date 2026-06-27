using Core.EventBus.Integration;
using Core.EventBus.Outbox;
using Core.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>RabbitMQ broker 的 <see cref="IEventBusOptionsExtensions"/> 实现;把 options 绑到 IOptions、桥接 connection 到底层模块,并完成 publisher/subscriber/池等 DI 注册。</summary>
    /// <remarks>支持 Action&lt;EventBusRabbitMqOptions&gt; 与 <see cref="IConfiguration"/> 两种来源,构造函数二选一。</remarks>
    public class EventBusOptionsExtensions : IEventBusOptionsExtensions
    {
        private readonly Action<EventBusRabbitMqOptions> _options;
        private readonly IConfiguration _configuration;

        public EventBusOptionsExtensions(Action<EventBusRabbitMqOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public EventBusOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>绑定 options 到 IOptions、把 connection / 失败策略 / DLX 桥接到底层 RabbitMQ 模块,并注册 publisher / subscriber / 池 / IOutboxRawSender。</summary>
        public void AddServices(IServiceCollection services)
        {
            var options = new EventBusRabbitMqOptions();
            if (_options != null)
            {
                services.Configure(_options);
                _options.Invoke(options);
            }
            else if (_configuration != null)
            {
                services.Configure<EventBusRabbitMqOptions>(_configuration);
                // 配置节点缺失/为空时 Get<T>() 返回 null,沿用默认实例避免下方 NRE
                var fromConfig = _configuration.Get<EventBusRabbitMqOptions>();
                if (fromConfig != null) options = fromConfig;
            }

            services.AddRabbitMq(rabbitMqOptions =>
            {
                rabbitMqOptions.Connection = options.Connection;
                // 失败策略桥接到底层 consumer 的 ack/nack 决策
                rabbitMqOptions.FailureBehavior = options.FailureBehavior;
                rabbitMqOptions.DeadLetterExchange = options.DeadLetterExchange;
                rabbitMqOptions.DeadLetterRoutingKey = options.DeadLetterRoutingKey;
            });

            // 用 EventBus 维度的 exchange / pool 大小实例化通用 channel 池
            services.TryAddSingleton<IRabbitMqPublishChannelPool>(sp =>
            {
                var ebOptions = sp.GetRequiredService<IOptions<EventBusRabbitMqOptions>>().Value;
                return new RabbitMqPublishChannelPool(
                    sp.GetRequiredService<IRabbitMqPersistentConnection>(),
                    ebOptions.ExchangeName,
                    ebOptions.ChannelPoolSize,
                    sp.GetRequiredService<ILogger<RabbitMqPublishChannelPool>>());
            });

            // publisher 注册为 Scoped:它在 PublishAsync 里用注入的 IServiceProvider 解析 IOutboxStorage,
            // 必须是当前请求 scope 的 SP,才能让 storage 创建/复用的 DbContext 由 UoW 所在 scope 持有,
            // 避免临时 scope dispose 导致外层 UoW 持有已释放的 DbContext。
            // subscriber 仍是 Singleton:管理 broker 长连接 / consumer。
            // IOutboxRawSender 同步降为 Scoped:它委托给 IIntegrationPublisher,从根容器解析 Scoped 会触发 scope-validation。
            // OutboxDispatcher 调用时本就在自建的 scope 内 → 兼容。
            services.TryAddScoped<IIntegrationPublisher, RabbitMqMessagePublisher>();
            services.TryAddSingleton<IIntegrationSubscriber, RabbitMqMessageSubscriber>();
            services.TryAddScoped(sp =>
                (IOutboxRawSender)sp.GetRequiredService<IIntegrationPublisher>());
            services.AddIntegrationCore();
        }
    }
}
