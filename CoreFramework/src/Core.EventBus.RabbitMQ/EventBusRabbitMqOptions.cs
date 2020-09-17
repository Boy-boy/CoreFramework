using System;
using System.Collections.Generic;

namespace Core.EventBus.RabbitMQ
{
    public class EventBusRabbitMqOptions
    {
        public RabbitMqPublishConfigure RabbitMqPublishConfigure { get; set; }


        public List<RabbitMqSubscribeConfigure> RabbitSubscribeConfigures { get; set; }

        public EventBusRabbitMqOptions()
        {
            RabbitMqPublishConfigure = new RabbitMqPublishConfigure();
            RabbitSubscribeConfigures = new List<RabbitMqSubscribeConfigure>();
        }


        public EventBusRabbitMqOptions AddPublishConfigure(Action<RabbitMqPublishConfigure> configureOptions)
        {
            if (configureOptions == null) return this;
            configureOptions.Invoke(RabbitMqPublishConfigure);
            return this;
        }

        public EventBusRabbitMqOptions AddSubscribeConfigures(Action<List<RabbitMqSubscribeConfigure>> configureOptions)
        {
            if (configureOptions == null) return this;
            configureOptions.Invoke(RabbitSubscribeConfigures);
            return this;
        }
    }
}
