namespace Core.EventBus.RabbitMQ
{
    public class RabbitMqPublishConfigure
    {
        public string ExchangeName { get; set; }

        public RabbitMqPublishConfigure()
        {
        }

        public string GetExchangeName()
        {
            return ExchangeName;
        }
    }
}
