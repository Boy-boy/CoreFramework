using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Core.Messaging
{
    public class MessageHandlerWrapper<TMessage> : IMessageHandlerWrapper
        where TMessage : class, IMessage
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessageHandler<TMessage> _handler;
        private readonly Type _handlerType;
        private readonly Type _baseHandlerType;


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

        public Type HandlerType => _handlerType;

        public Type BaseHandlerType => _baseHandlerType;

        public int HandlerPriority { get; }

        public Task HandlerAsync(IMessage message)
        {
            return _handler != null
                ? _handler.HandAsync(message as TMessage)
                : GetIocMessageHandler().HandAsync(message as TMessage);
        }

        private IMessageHandler<TMessage> GetIocMessageHandler()
        {
            return (IMessageHandler<TMessage>)_serviceScopeFactory
                .CreateScope()
                .ServiceProvider
                .GetRequiredService(_baseHandlerType);
        }
    }
}
