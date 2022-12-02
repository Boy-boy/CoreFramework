using Core.EventBus.Diagnostics;
using Core.EventBus.Integration;
using Core.Json.Newtonsoft;
using Core.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Core.EventBus.RabbitMQ
{
    public class RabbitMqMessagePublisher : IntegrationMessagePublisherBase, IIntegrationMessagePublisher
    {
        private readonly int _retryCount = 3;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IOptions<EventBusRabbitMqOptions> _options;
        private readonly ILogger<RabbitMqMessagePublisher> _logger;

        public RabbitMqMessagePublisher(
            IServiceProvider serviceProvider,
            IRabbitMqPersistentConnection persistentConnection,
            IOptions<EventBusRabbitMqOptions> options,
            ILogger<RabbitMqMessagePublisher> logger)
        : base(serviceProvider)
        {
            _persistentConnection = persistentConnection;
            _options = options;
            _logger = logger;
        }

        public override async Task SendAsync<T>(T message)
        {
            _logger.LogTrace("Enable diagnostic listeners before publishing,name is {name}", DiagnosticListenerConstants.BeforePublish);
            EventBusDiagnosticListener.TracingPublishBefore(message);

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(retryAttempt), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", message.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var messageName = MessageNameAttribute.GetNameOrDefault(message.GetType());
            var data = message.ToJson();
            var body = Encoding.UTF8.GetBytes(data).AsMemory();

            var exchangeName = _options.Value.ExchangeName;

            policy.Execute(() =>
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                using var channel = _persistentConnection.CreateModel();
                var model = channel;
                _logger.LogTrace("Declaring RabbitMQ exchange {ExchangeName} to publish event: {EventId}", exchangeName, message.Id);
                model.ExchangeDeclare(exchange: exchangeName, type: "direct", durable: true, autoDelete: false,
                    arguments: new ConcurrentDictionary<string, object>());

                var properties = model.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent
                _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", message.Id);
                model.BasicPublish(
                    exchange: exchangeName,
                    routingKey: messageName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });

            _logger.LogTrace("Enable diagnostic listeners after publishing,name is {name}", DiagnosticListenerConstants.AfterPublish);
            EventBusDiagnosticListener.TracingPublishAfter(message);
            await Task.CompletedTask;
        }
    }
}
