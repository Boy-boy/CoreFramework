using System;
using Core.EventBus;
using Core.EventBus.Kafka;
using Microsoft.Extensions.Configuration;
using EventBusOptionsExtensions = Core.EventBus.Kafka.EventBusOptionsExtensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 提供 <c>options.AddKafka(...)</c> 流式扩展,把 Kafka broker 加入 EventBus 扩展链。
    /// </summary>
    public static class EventBusKafkaServiceCollectionExtensions
    {
        /// <summary>用委托配置 Kafka,适合代码侧组装/测试。</summary>
        public static EventBusOptions AddKafka(this EventBusOptions options, Action<EventBusKafkaOptions> actionOptions)
        {
            if (actionOptions == null) throw new ArgumentNullException(nameof(actionOptions));

            options.AddExtensions(new EventBusOptionsExtensions(actionOptions));
            return options;
        }

        /// <summary>用配置节点配置 Kafka,通常传 <c>Configuration.GetSection("EventBus:Kafka")</c>。</summary>
        public static EventBusOptions AddKafka(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            options.AddExtensions(new EventBusOptionsExtensions(configuration));
            return options;
        }
    }
}
