using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.EventBus
{
    public class MessageHandlerWrapper<TMessage> : IMessageHandlerWrapper
        where TMessage : class, IMessage
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessageHandler<TMessage> _handler;
        private readonly Type _handlerType;
        private readonly Type _baseHandlerType;

        public MessageHandlerWrapper(){}
        public MessageHandlerWrapper(
            IServiceScopeFactory serviceScopeFactory,
            Type handlerType,
            Type baseHandlerType)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _handlerType = handlerType;
            _baseHandlerType = baseHandlerType;
            HandlerPriority = MessageHandlerPriorityAttribute.GetPriority(typeof(TMessage), _handlerType);
            if (MessageHandlerLifetimeAttribute.GetHandlerLifetime(_handlerType) == MessageHandlerLifetime.Singleton)
            {
                _handler = GetIocMessageHandler();
            }
        }

        public IMessageHandler Handler => _handler ?? GetIocMessageHandler();

        public Type HandlerType => _handlerType;

        public Type BaseHandlerType => _baseHandlerType;

        public int HandlerPriority { get; }

        private IMessageHandler<TMessage> GetIocMessageHandler()
        {
            return (IMessageHandler<TMessage>)_serviceScopeFactory
                .CreateScope()
                .ServiceProvider
                .GetRequiredService(_baseHandlerType);
        }
    }
}
