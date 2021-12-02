namespace Core.RabbitMQ
{
    public interface IRabbitMqMessageConsumerManager
    {
        IRabbitMqMessageConsumer TryCreate(RabbitMqExchangeDeclareConfigure exchangeDeclare,
            RabbitMqQueueDeclareConfigure queueDeclare);

        bool TryGet(string exchangeName, string queueName, out IRabbitMqMessageConsumer consumer);

        bool TryRemove(string exchangeName, string queueName);
    }
}
