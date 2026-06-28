using Core.EventBus.Integration;
using Core.EventBus.Outbox;
using Core.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

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
            // 同一进程仅允许一个 integration broker;检测到其他实现则抛错
            GuardSingleIntegrationBroker(services);

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

            // 用 EventBus 维度的 exchange / pool 大小实例化通用 channel 池。
            // 构造细节封装在 Core.RabbitMQ.AddRabbitMqPublishChannelPool 内,本层只提供 accessor。
            services.AddRabbitMqPublishChannelPool(
                sp => sp.GetRequiredService<IOptions<EventBusRabbitMqOptions>>().Value.ExchangeName,
                sp => sp.GetRequiredService<IOptions<EventBusRabbitMqOptions>>().Value.ChannelPoolSize);

            // publisher / IOutboxRawSender 用 Scoped:与 UoW 所在 scope 对齐,避免 outbox 写入后 DbContext 提前释放
            // subscriber 保留 Singleton:管理 broker 长连接
            services.TryAddScoped<IIntegrationPublisher, RabbitMqMessagePublisher>();
            services.TryAddSingleton<IIntegrationSubscriber, RabbitMqMessageSubscriber>();
            services.TryAddScoped(sp =>
                (IOutboxRawSender)sp.GetRequiredService<IIntegrationPublisher>());
            services.AddIntegrationCore();
        }

        /// <summary>已注册非自身实现 → 抛错;同 broker 重复调用幂等放行。工厂 / 实例注册也拒绝:IOutboxRawSender 会基于 IIntegrationPublisher 强转,自定义 publisher 不实现 IOutboxRawSender 将在运行时抛 InvalidCastException;需要自定义请同时显式注册 IOutboxRawSender 并跳过 AddRabbitMq/AddKafka。</summary>
        private static void GuardSingleIntegrationBroker(IServiceCollection services)
        {
            var existing = services.FirstOrDefault(s => s.ServiceType == typeof(IIntegrationPublisher));
            if (existing == null) return;
            if (existing.ImplementationType == typeof(RabbitMqMessagePublisher)) return;
            var label = existing.ImplementationType?.Name
                ?? (existing.ImplementationFactory != null ? "<factory>" : "<instance>");
            throw new InvalidOperationException(
                $"已注册 IIntegrationPublisher = {label};EventBus 同一时刻仅支持一个 integration broker,请只调用 AddRabbitMq / AddKafka 之一。自定义实现需自行注册 IIntegrationPublisher + IOutboxRawSender 并跳过 AddRabbitMq/AddKafka。");
        }
    }
}
