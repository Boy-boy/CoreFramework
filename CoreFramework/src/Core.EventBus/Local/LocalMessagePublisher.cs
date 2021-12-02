using System;
using System.Linq;
using System.Threading.Tasks;
using Core.EventBus.Messaging;
using Core.EventBus.Messaging.Diagnostics;
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
            await Task.Yield();
            var messageHandlers = _messageHandlerProvider
                .GetHandlers<T>()
                .ToList();

            if (messageHandlers.Any())
            {
                _logger.LogTrace("Enable diagnostic listeners before consume,name is {name}", DiagnosticListenerConstants.BeforeConsume);
                EventBusDiagnosticListener.TracingConsumeBefore(message);

                foreach (var messageHandler in messageHandlers)
                {
                    var concreteType = typeof(IMessageHandler<>).MakeGenericType(typeof(T));
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
