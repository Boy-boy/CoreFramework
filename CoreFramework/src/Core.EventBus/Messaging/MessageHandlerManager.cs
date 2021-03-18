using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.EventBus
{
    public class MessageHandlerManager : IMessageHandlerManager
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDictionary<Type, IList<IMessageHandlerWrapper>> _handlerDict;
        private readonly IDictionary<string, Type> _messageTypeMappingDict;

        public event EventHandler<Type> OnEventRemoved;
        public IDictionary<Type, IList<IMessageHandlerWrapper>> MessageHandlerDict => _handlerDict;
        public IDictionary<string, Type> MessageTypeMappingDict => _messageTypeMappingDict;

        public MessageHandlerManager(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _handlerDict = new Dictionary<Type, IList<IMessageHandlerWrapper>>();
            _messageTypeMappingDict = new Dictionary<string, Type>();
        }

        public void AddHandler(Type messageType, Type handlerType)
        {
            var baseHandlerTypes = MessageHandlerExtensions.GetBaseHandlerTypes(handlerType);
            foreach (var baseHandlerType in baseHandlerTypes)
            {
                var messageType1 = baseHandlerType.GenericTypeArguments.Single();
                if (messageType != messageType1) continue;
                RegisterMessageHandlerWrapper(messageType, handlerType, baseHandlerType);
                RegisterMessageTypeMapping(messageType);
            }
        }

        public void RemoveHandler(Type messageType, Type handlerType)
        {
            var baseHandlerTypes = MessageHandlerExtensions.GetBaseHandlerTypes(handlerType);
            foreach (var baseHandlerType in baseHandlerTypes)
            {
                var messageType1 = baseHandlerType.GenericTypeArguments.Single();
                if (messageType != messageType1) continue;
                if (!_handlerDict.TryGetValue(messageType, out var handlers)) continue;
                if (handlers.All(handlerWrapper => handlerWrapper.BaseHandlerType != baseHandlerType)) continue;
                handlers = handlers.Where(x => x.BaseHandlerType != baseHandlerType).ToList();
                if (handlers.Count != 0) continue;
                _handlerDict.Remove(messageType);
                var eventName = MessageNameAttribute.GetNameOrDefault(messageType);
                if (_messageTypeMappingDict.ContainsKey(eventName))
                    _messageTypeMappingDict.Remove(eventName);
                OnEventRemoved?.Invoke(this, messageType);
            }
        }

        private void RegisterMessageHandlerWrapper(Type messageType, Type handlerType, Type baseHandlerType)
        {
            if (!_handlerDict.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<IMessageHandlerWrapper>();
                _handlerDict.Add(messageType, handlers);
            }
            if (handlers.Any(handlerWrapper => handlerWrapper.BaseHandlerType == baseHandlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {baseHandlerType.Name} already registered for '{messageType.Name}'");
            }
            var handlerWrapperType = typeof(MessageHandlerWrapper<>).MakeGenericType(messageType);
            handlers.Add(Activator.CreateInstance(handlerWrapperType, _serviceScopeFactory, handlerType, baseHandlerType) as IMessageHandlerWrapper);
        }

        private void RegisterMessageTypeMapping(Type messageType)
        {
            var eventName = MessageNameAttribute.GetNameOrDefault(messageType);
            if (!_messageTypeMappingDict.ContainsKey(eventName))
                _messageTypeMappingDict.Add(eventName, messageType);
        }
    }
}
