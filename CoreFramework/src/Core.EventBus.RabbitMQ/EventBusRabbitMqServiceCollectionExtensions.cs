using Core.EventBus;
using Core.EventBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using System;
using EventBusOptionsExtensions = Core.EventBus.RabbitMQ.EventBusOptionsExtensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary><c>options.AddRabbitMq(...)</c> 流式扩展;支持 Action 与 IConfiguration 两种配置来源。</summary>
    public static class EventBusRabbitMqServiceCollectionExtensions
    {
        /// <summary>用 Action 配置 RabbitMQ,适合代码侧组装/测试。</summary>
        public static EventBusOptions AddRabbitMq(this EventBusOptions options, Action<EventBusRabbitMqOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));

            options.AddExtensions(new EventBusOptionsExtensions(actionOptions));
            return options;
        }

        /// <summary>用 IConfiguration 节点(通常是 <c>EventBus:RabbitMq</c>)配置 RabbitMQ。</summary>
        public static EventBusOptions AddRabbitMq(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            options.AddExtensions(new EventBusOptionsExtensions(configuration));
            return options;
        }
    }
}
