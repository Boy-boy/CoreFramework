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
    /// <summary>RabbitMQ broker 的 <see cref="IEventBusOptionsExtensions"/> 实现;把 options 绑到 IOptions、用 IOptions 联动把 <see cref="EventBusRabbitMqOptions.Broker"/> 字段透传给底层 <see cref="RabbitMqOptions"/>,并完成 publisher/subscriber/池等 DI 注册。</summary>
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

        /// <summary>绑定 EventBus 侧 options,用 IOptions 联动把 <see cref="EventBusRabbitMqOptions.Broker"/> 透传给底层 <see cref="RabbitMqOptions"/>,并注册 publisher / subscriber / 池 / IOutboxRawSender。</summary>
        public void AddServices(IServiceCollection services)
        {
            // 同一进程仅允许一个 integration broker;检测到其他实现则抛错
            GuardSingleIntegrationBroker(services);

            if (_options != null)
            {
                services.Configure(_options);
            }
            else if (_configuration != null)
            {
                services.Configure<EventBusRabbitMqOptions>(_configuration);
            }
            // PostConfigure 在所有 Configure 跑完后由 IOptions 解析触发;publisher/subscriber 启动时第一次解析即生效
            services.PostConfigure<EventBusRabbitMqOptions>(o => o.Validate());

            // 只注册 infrastructure singleton,Configure<RabbitMqOptions> 由下面的 IOptions 联动接管
            services.AddRabbitMq();

            // 用 IOptions 联动取代之前的快照式手写桥接:解析 RabbitMqOptions 时拉一份 EventBusRabbitMqOptions.Broker 字段
            // 1) 后续对 EventBusRabbitMqOptions 的 PostConfigure 同样能传递到 RabbitMqOptions
            // 2) Broker 是 RabbitMqOptions 原型,底层新增字段无需 EventBus 层跟改 —— 这里不做字段映射,直接整对象复制
            services.AddOptions<RabbitMqOptions>().Configure<IOptions<EventBusRabbitMqOptions>>((rmq, eb) =>
            {
                var src = eb.Value.Broker;
                if (src == null) return;
                rmq.Connection = src.Connection;
                rmq.FailureBehavior = src.FailureBehavior;
                rmq.DeadLetterExchange = src.DeadLetterExchange;
                rmq.DeadLetterRoutingKey = src.DeadLetterRoutingKey;
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
