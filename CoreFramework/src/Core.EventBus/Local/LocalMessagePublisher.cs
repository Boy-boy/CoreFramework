using System.Linq;
using System.Threading.Tasks;
using Core.EventBus.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.EventBus.Local
{
    public class LocalMessagePublisher : MessagePublisherBase
    {
        private readonly ILogger<LocalMessagePublisher> _logger;
        private readonly IMessageHandlerProvider _messageHandlerProvider;

        public LocalMessagePublisher(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<LocalMessagePublisher> logger,
            IMessageHandlerProvider messageHandlerProvider)
        : base(serviceScopeFactory)
        {
            _logger = logger;
            _messageHandlerProvider = messageHandlerProvider;
        }

        public override async Task SendAsync<T>(T message)
        {
            var messageHandlers = _messageHandlerProvider
                .GetHandlers<T>()
                .ToList();

            if (messageHandlers.Any())
            {
                foreach (var messageHandler in messageHandlers)
                {
                    var concreteType = typeof(IMessageHandler<>).MakeGenericType(typeof(T));
                    var method = concreteType.GetMethod("HandAsync");
                    if (method != null)
                    {
                        await (Task)method.Invoke(messageHandler, new object[] { message });
                    }
                }
            }
            else
            {
                var messageName = MessageNameAttribute.GetNameOrDefault(message.GetType());
                _logger.LogWarning("No subscription for local memory message: {eventName}", messageName);
            }
            await Task.CompletedTask;
        }
    }
}
