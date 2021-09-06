using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.EventBus
{
    public class MessageHandlerManager : IMessageHandlerManager
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IList<IMessageHandlerWrapper> _messageHandlerWrappers;

        public event EventHandler<Type> OnEventRemoved;
        public IList<IMessageHandlerWrapper> MessageHandlerWrappers => _messageHandlerWrappers;

        public MessageHandlerManager(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _messageHandlerWrappers = new List<IMessageHandlerWrapper>();
        }

        public void AddHandler(Type messageType, Type handlerType)
        {
            if (_messageHandlerWrappers.Any(handlerWrapper => handlerWrapper.MessageType == messageType && handlerWrapper.HandlerType == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{messageType.Name}'");
            }

            var messageName = MessageNameAttribute.GetNameOrDefault(messageType);
            if (_messageHandlerWrappers.Any(handlerWrapper => handlerWrapper.MessageName == messageName && handlerWrapper.MessageType != messageType))
            {
                throw new ArgumentException(
                    $"The message name '{messageName}' corresponding to the message type '{messageType}' already exists");
            }

            var handlerWrapperType = typeof(MessageHandlerWrapper<>).MakeGenericType(messageType);
            _messageHandlerWrappers.Add(Activator.CreateInstance(handlerWrapperType, _serviceScopeFactory, handlerType) as IMessageHandlerWrapper);
        }

        public void RemoveHandler(Type messageType, Type handlerType)
        {
            var handler = _messageHandlerWrappers
                .FirstOrDefault(p => p.MessageType == messageType && p.HandlerType == handlerType);

            if (handler != null)
                _messageHandlerWrappers.Remove(handler);

            if (_messageHandlerWrappers.Any(p => p.MessageType == messageType))
                return;

            OnEventRemoved?.Invoke(this, messageType);
        }
    }
}
