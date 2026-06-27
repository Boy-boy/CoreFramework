using System;
using Core.EventBus;
using Core.EventBus.Kafka;
using Microsoft.Extensions.Configuration;
using EventBusOptionsExtensions = Core.EventBus.Kafka.EventBusOptionsExtensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 提供 <c>options.AddKafka(...)</c> 流式扩展，把 Kafka broker 加入 EventBus 扩展链。
    /// 镜像 <c>EventBusRabbitMqServiceCollectionExtensions</c>。
    /// </summary>
    public static class EventBusKafkaServiceCollectionExtensions
    {
        /// <summary>
        /// 用 <c>Action&lt;EventBusKafkaOptions&gt;</c> 形式配置 Kafka。适合代码侧组装 / 测试场景。
        /// </summary>
        public static EventBusOptions AddKafka(this EventBusOptions options, Action<EventBusKafkaOptions> actionOptions)
        {
            if (actionOptions == null) throw new ArgumentNullException(nameof(actionOptions));

            options.AddExtensions(new EventBusOptionsExtensions(actionOptions));
            return options;
        }

        /// <summary>
        /// 用 appsettings.json 节点配置 Kafka。<paramref name="configuration"/> 通常是
        /// <c>Configuration.GetSection("EventBus:Kafka")</c>。
        /// </summary>
        public static EventBusOptions AddKafka(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            options.AddExtensions(new EventBusOptionsExtensions(configuration));
            return options;
        }
    }
}
