using Core.EventBus.Messaging;
using Core.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Core.EventBus.RabbitMQ
{
    public class RabbitMqMessagePublisher : MessagePublisherBase
    {
        private readonly int _retryCount = 5;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IOptions<EventBusRabbitMqOptions> _options;
        private readonly ILogger<RabbitMqMessagePublisher> _logger;

        public RabbitMqMessagePublisher(
            IServiceScopeFactory serviceScopeFactory,
            IRabbitMqPersistentConnection persistentConnection,
            IOptions<EventBusRabbitMqOptions> options,
            ILogger<RabbitMqMessagePublisher> logger)
        : base(serviceScopeFactory)
        {
            _persistentConnection = persistentConnection;
            _options = options;
            _logger = logger;
        }

        public override Task SendAsync<T>(T message)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(retryAttempt), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", message.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var eventName = MessageNameAttribute.GetNameOrDefault(message.GetType());
            _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", message.Id, eventName);

            using (var channel = _persistentConnection.CreateModel())
            {
                _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", message.Id);
                var data = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(data).AsMemory();

                var model = channel;
                var exchangeName = _options.Value.RabbitMqPublishConfigure.GetExchangeName() ?? RabbitMqConst.DefaultExchangeName;
                model.ExchangeDeclare(exchange: exchangeName, type: "direct", durable: true, autoDelete: false, arguments: new ConcurrentDictionary<string, object>());
                policy.Execute(() =>
                {
                    var properties = model.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent
                    _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", message.Id);
                    model.BasicPublish(
                        exchange: exchangeName,
                        routingKey: eventName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);
                });
            }
            return Task.CompletedTask;
        }
    }
}
