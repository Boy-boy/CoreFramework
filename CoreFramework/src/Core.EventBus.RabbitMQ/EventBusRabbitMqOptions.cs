using Core.RabbitMQ;
using System;

namespace Core.EventBus.RabbitMQ
{
    public class EventBusRabbitMqOptions
    {
        private string _defaultExchangeName = "event_bus_default_routing";

        public string ExchangeName
        {
            get => _defaultExchangeName;
            set => _defaultExchangeName = value ?? throw new Exception("exchange is not allowed to be null");
        }

        public EventBusRabbitMqOptions()
        {
            RabbitMqConnection = new RabbitMqConnectionConfigure();
        }

        public RabbitMqConnectionConfigure RabbitMqConnection { get; set; }
    }
}
