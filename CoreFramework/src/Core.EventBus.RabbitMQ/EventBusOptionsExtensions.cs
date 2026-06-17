using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Core.EventBus.Integration;
using Core.EventBus.Outbox;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                options = _configuration.Get<EventBusRabbitMqOptions>();
            }

            services.AddRabbitMq(rabbitMqOptions =>
            {
                rabbitMqOptions.Connection = options.Connection;
            });

            services.TryAddSingleton<IIntegrationMessagePublisher, RabbitMqMessagePublisher>();
            services.TryAddSingleton<IIntegrationMessageSubscribe, RabbitMqMessageSubscribe>();
            services.TryAddSingleton(sp =>
                (IOutboxRawSender)sp.GetRequiredService<IIntegrationMessagePublisher>());
            services.AddIntegrationCore();
        }
    }
}
