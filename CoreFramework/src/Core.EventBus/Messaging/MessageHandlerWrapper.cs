using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Messaging
{
    public class MessageHandlerWrapper<TMessage> : IMessageHandlerWrapper
        where TMessage : class, IMessage
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessageHandler<TMessage> _handler;
        private readonly Type _baseHandlerType;


        public MessageHandlerWrapper(
            IServiceScopeFactory serviceScopeFactory,
            Type baseHandlerType)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _baseHandlerType = baseHandlerType;

            if (MessageHandlerLifetimeAttribute.GetHandlerLifetime(baseHandlerType) == MessageHandlerLifetime.Singleton)
            {
                _handler = GetIocMessageHandler();
            }
        }

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
