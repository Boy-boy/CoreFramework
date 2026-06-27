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
    /// <summary>
    /// RabbitMQ broker 的 <see cref="IEventBusOptionsExtensions"/> 实现：
    /// 仅负责把 <see cref="EventBusRabbitMqOptions"/> 绑到 IOptions，并把 connection 配置桥接到底层 <c>CoreRabbitMqModule</c>。
    /// publisher / subscribe / IOutboxRawSender 等服务的注册由 <see cref="CoreEventBusRabbitMqModule"/> 完成，不在这里重复。
    /// </summary>
    /// <remarks>
    /// 支持两种配置来源：
    /// <list type="bullet">
    ///   <item><description>代码侧 <c>Action&lt;EventBusRabbitMqOptions&gt;</c>：用于测试 / 程序内组装。</description></item>
    ///   <item><description><see cref="IConfiguration"/> 节点：典型用于 appsettings.json 驱动。</description></item>
    /// </list>
    /// 两者互斥（构造函数二选一），<see cref="AddServices"/> 中按设置的那个执行绑定。
    /// </remarks>
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

        /// <summary>
        /// 把 RabbitMQ 配置绑到 IOptions，并把 connection 信息桥接到底层 RabbitMQ 模块。
        /// 由 <see cref="EventBusOptionsExtensions.Configure"/> 在 PostConfigureServices 阶段调用。
        /// </summary>
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
                // 配置节点缺失 / 为空时 Get<T>() 返回 null;沿用上面 new 出的默认实例,
                // 否则下面 options.Connection 会 NRE
                var fromConfig = _configuration.Get<EventBusRabbitMqOptions>();
                if (fromConfig != null) options = fromConfig;
            }

            services.AddRabbitMq(rabbitMqOptions =>
            {
                rabbitMqOptions.Connection = options.Connection;
                // 把 EventBus 维度的失败策略桥接到底层 consumer 的 ack/nack 决策
                rabbitMqOptions.FailureBehavior = options.FailureBehavior;
                rabbitMqOptions.DeadLetterExchange = options.DeadLetterExchange;
                rabbitMqOptions.DeadLetterRoutingKey = options.DeadLetterRoutingKey;
            });

            // 把 EventBus 维度的 exchange / pool 大小桥接成"具体实例"注册给底层池接口 ——
            // 池本身在 Core.RabbitMQ 是通用基础设施,只是这里用 EventBus 配置实例化它
            services.TryAddSingleton<IRabbitMqPublishChannelPool>(sp =>
            {
                var ebOptions = sp.GetRequiredService<IOptions<EventBusRabbitMqOptions>>().Value;
                return new RabbitMqPublishChannelPool(
                    sp.GetRequiredService<IRabbitMqPersistentConnection>(),
                    ebOptions.ExchangeName,
                    ebOptions.ChannelPoolSize,
                    sp.GetRequiredService<ILogger<RabbitMqPublishChannelPool>>());
            });

            services.TryAddSingleton<IIntegrationPublisher, RabbitMqMessagePublisher>();
            services.TryAddSingleton<IIntegrationSubscriber, RabbitMqMessageSubscriber>();
            services.TryAddSingleton(sp =>
                (IOutboxRawSender)sp.GetRequiredService<IIntegrationPublisher>());
            services.AddIntegrationCore();
        }
    }
}
