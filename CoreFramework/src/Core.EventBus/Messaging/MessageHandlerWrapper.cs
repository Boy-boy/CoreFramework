using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.EventBus
{
    public class MessageHandlerWrapper<TMessage> : IMessageHandlerWrapper
        where TMessage : class, IMessage
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessageHandler<TMessage> _handler;

        public MessageHandlerWrapper() { }
        public MessageHandlerWrapper(
            IServiceScopeFactory serviceScopeFactory,
            Type handlerType)
        {
            _serviceScopeFactory = serviceScopeFactory;
            MessageName = MessageNameAttribute.GetNameOrDefault(typeof(TMessage));
            MessageType = typeof(TMessage);
            HandlerType = handlerType;
            HandlerPriority = MessageHandlerPriorityAttribute.GetPriority(typeof(TMessage), handlerType);
            if (MessageHandlerLifetimeAttribute.GetHandlerLifetime(handlerType) == MessageHandlerLifetime.Singleton)
            {
                _handler = GetIocMessageHandler();
            }
        }

        public IMessageHandler Handler => _handler ?? GetIocMessageHandler();

        public string MessageName { get; }

        public Type MessageType { get; }

        public Type HandlerType { get; }

        public int HandlerPriority { get; }

        private IMessageHandler<TMessage> GetIocMessageHandler()
        {
            return (IMessageHandler<TMessage>)_serviceScopeFactory
                .CreateScope()
                .ServiceProvider
                .GetRequiredService(HandlerType);
        }
    }
}
