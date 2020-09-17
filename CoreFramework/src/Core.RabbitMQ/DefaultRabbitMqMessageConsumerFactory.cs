using Microsoft.Extensions.DependencyInjection;

namespace Core.RabbitMQ
{
    public class DefaultRabbitMqMessageConsumerFactory : IRabbitMqMessageConsumerFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DefaultRabbitMqMessageConsumerFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public IRabbitMqMessageConsumer Create(RabbitMqExchangeDeclareConfigure exchangeDeclare, RabbitMqQueueDeclareConfigure queueDeclare)
        {
            var consumer = (DefaultRabbitMqMessageConsumer)ActivatorUtilities.CreateInstance(_serviceScopeFactory.CreateScope().ServiceProvider,
                typeof(DefaultRabbitMqMessageConsumer));
            consumer.Initialize(exchangeDeclare, queueDeclare);
            return consumer;
        }
    }
}
