using Core.EventBus;
using Core.EventBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using System;
using EventBusOptionsExtensions = Core.EventBus.RabbitMQ.EventBusOptionsExtensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 提供 <c>options.AddRabbitMq(...)</c> 流式扩展，把 RabbitMQ broker 加入 EventBus 扩展链。
    /// 支持 Action 配置与 IConfiguration 两种来源。
    /// </summary>
    public static class EventBusRabbitMqServiceCollectionExtensions
    {
        /// <summary>
        /// 用 <c>Action&lt;EventBusRabbitMqOptions&gt;</c> 形式配置 RabbitMQ。适合代码侧组装 / 测试场景。
        /// </summary>
        public static EventBusOptions AddRabbitMq(this EventBusOptions options, Action<EventBusRabbitMqOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));

            options.AddExtensions(new EventBusOptionsExtensions(actionOptions));
            return options;
        }

        /// <summary>
        /// 用 appsettings.json 节点配置 RabbitMQ。<paramref name="configuration"/> 通常是
        /// <c>Configuration.GetSection("EventBus:RabbitMq")</c>。
        /// </summary>
        public static EventBusOptions AddRabbitMq(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            options.AddExtensions(new EventBusOptionsExtensions(configuration));
            return options;
        }
    }
}
