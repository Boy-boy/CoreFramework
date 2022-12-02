using Core.EventBus.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.EventBus.Local
{
    public class LocalMessagePublisher : ILocalMessagePublisher
    {
        private readonly ILogger<LocalMessagePublisher> _logger;
        private readonly ILocalMessageHandlerProvider _messageHandlerProvider;

        public LocalMessagePublisher(
            ILogger<LocalMessagePublisher> logger,
            ILocalMessageHandlerProvider messageHandlerProvider)
        {
            _logger = logger;
            _messageHandlerProvider = messageHandlerProvider;
        }

        public async Task PublishAsync<T>(T message)
            where T : class, IMessage
        {
            var messageHandlers = _messageHandlerProvider
                 .GetHandlers(message.GetType())
                 .ToList();

            if (messageHandlers.Any())
            {
                _logger.LogTrace("Enable diagnostic listeners before consume,name is {name}", DiagnosticListenerConstants.BeforeConsume);
                EventBusDiagnosticListener.TracingConsumeBefore(message);

                foreach (var messageHandler in messageHandlers)
                {
                    var concreteType = typeof(IMessageHandler<>).MakeGenericType(message.GetType());
                    var method = concreteType.GetMethod("HandAsync");
                    if (method == null) continue;
                    try
                    {
                        await (Task)method.Invoke(messageHandler, new object[] { message });
                    }
                    catch (Exception e)
                    {
                        var handlerType = messageHandler.GetType();
                        var messageType = message.GetType();
                        _logger.LogError("Message processing failure,message type is {messageType},handler type is {handlerType},error message is {errorMessage}",
                            messageType, handlerType, e.Message);

                        _logger.LogTrace("Enable diagnostic listeners incorrect consume,name is {name}", DiagnosticListenerConstants.ErrorConsume);
                        EventBusDiagnosticListener.TracingConsumeError(message, handlerType, e.Message);
                    }
                }

                _logger.LogTrace("Enable diagnostic listeners after consume,name is {name}", DiagnosticListenerConstants.AfterConsume);
                EventBusDiagnosticListener.TracingConsumeAfter(message);
            }
            else
            {
                var messageName = MessageNameAttribute.GetNameOrDefault(message.GetType());
                _logger.LogWarning("No subscription for local memory message: {eventName}", messageName);

                _logger.LogTrace("Not subscribed to enable diagnostic listener,name is {name}", DiagnosticListenerConstants.NotSubscribed);
                EventBusDiagnosticListener.TracingNotSubscribed(message);
            }
            await Task.CompletedTask;
        }

    }
}
