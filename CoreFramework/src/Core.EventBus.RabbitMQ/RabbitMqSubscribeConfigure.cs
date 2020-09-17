using System;

namespace Core.EventBus.RabbitMQ
{
    public class RabbitMqSubscribeConfigure
    {
        public Type EventType { get; set; }
        public string ExchangeName { get; set; }
        public string QueueName { get; set; }

        public RabbitMqSubscribeConfigure(Type eventType, string exchangeName, string queueName)
        {
            EventType = eventType;
            ExchangeName = exchangeName;
            QueueName = queueName;
        }
    }
}
