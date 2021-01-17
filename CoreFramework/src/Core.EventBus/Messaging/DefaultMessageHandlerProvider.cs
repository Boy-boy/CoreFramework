using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Messaging
{
    public class DefaultMessageHandlerProvider : IMessageHandlerProvider
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDictionary<Type, IList<IMessageHandlerWrapper>> _handlerDict;

        public DefaultMessageHandlerProvider(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _handlerDict = new Dictionary<Type, IList<IMessageHandlerWrapper>>();
        }

        public IEnumerable<IMessageHandlerWrapper> GetHandlers(Type messageType)
        {
            return _handlerDict.ContainsKey(messageType)
                ? _handlerDict[messageType]
                    .OrderByDescending(x => x.HandlerPriority)
                    .ToList()
                : new List<IMessageHandlerWrapper>();
        }

        public void AddHandler(Type messageType, Type handlerType)
        {
            var baseHandlerTypes = GetBaseHandlerTypes(handlerType);
            foreach (var baseHandlerType in baseHandlerTypes)
            {
                var messageType1 = baseHandlerType.GenericTypeArguments.Single();
                if (messageType != messageType1) continue;
                RegisterMessageHandlerWrapper(messageType, handlerType, baseHandlerType);
            }
        }

        public void RemoveHandler(Type messageType, Type handlerType)
        {
            var baseHandlerTypes = GetBaseHandlerTypes(handlerType);
            foreach (var baseHandlerType in baseHandlerTypes)
            {
                var messageType1 = baseHandlerType.GenericTypeArguments.Single();
                if (messageType != messageType1) continue;
                if (!_handlerDict.TryGetValue(messageType, out var handlers)) continue;
                if (handlers.All(handlerWrapper => handlerWrapper.BaseHandlerType != baseHandlerType)) continue;
                handlers = handlers.Where(x => x.BaseHandlerType != baseHandlerType).ToList();
                if (handlers.Count == 0)
                {
                    _handlerDict.Remove(messageType);
                }
            }
        }

        public void Initialize(params Assembly[] assemblies)
        {
            var handlerTypes = GetHandlerTypes(assemblies);
            foreach (var handlerType in handlerTypes)
            {
                var baseHandlerTypes = GetBaseHandlerTypes(handlerType);
                foreach (var baseHandlerType in baseHandlerTypes)
                {
                    var messageType = baseHandlerType.GenericTypeArguments.Single();
                    RegisterMessageHandlerWrapper(messageType, handlerType, baseHandlerType);
                }
            }
        }

        private IEnumerable<Type> GetHandlerTypes(Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                return new List<Type>();
            return assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => typeof(IMessageHandler).GetTypeInfo().IsAssignableFrom(t))
                .ToList();
        }

        private IEnumerable<Type> GetBaseHandlerTypes(Type handlerType)
        {
            var baseHandlerTypes = handlerType
                .GetInterfaces()
                .Where(t =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
            baseHandlerTypes = baseHandlerTypes.Distinct();
            return baseHandlerTypes;
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
            var handlerWrapperType = GetHandlerWrapperImplementationType(baseHandlerType);
            handlers.Add(Activator.CreateInstance(handlerWrapperType, _serviceScopeFactory, handlerType, baseHandlerType) as IMessageHandlerWrapper);
        }

        private Type GetHandlerWrapperImplementationType(Type baseHandlerType)
        {
            return typeof(MessageHandlerWrapper<>).MakeGenericType(baseHandlerType.GetGenericArguments().Single());
        }
    }
}
