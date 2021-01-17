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

        public void Initialize(params Assembly[] assemblies)
        {
            if (assemblies == null) return;
            var handlerTypes = assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => typeof(IMessageHandler).GetTypeInfo().IsAssignableFrom(t))
                .ToList();
            foreach (var handlerType in handlerTypes)
            {
                RegisterBaseHandler(handlerType);
            }
        }

        private void RegisterBaseHandler(Type handlerType)
        {
            var baseHandlerTypes = handlerType
                .GetInterfaces()
                .Where(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
            baseHandlerTypes = baseHandlerTypes.Distinct();
            foreach (var baseHandlerType in baseHandlerTypes)
            {
                var messageType = baseHandlerType.GenericTypeArguments.Single();

                if (!_handlerDict.TryGetValue(messageType, out var handlers))
                {
                    handlers = new List<IMessageHandlerWrapper>();
                    _handlerDict.Add(messageType, handlers);
                }
                if (handlers.Any(handlerWrapper => handlerWrapper.HandlerType == baseHandlerType))
                {
                    throw new ArgumentException(
                        $"Handler Type {baseHandlerType.Name} already registered for '{messageType.Name}'");
                }
                var handlerWrapperType = GetHandlerWrapperImplementationType(baseHandlerType);
                handlers.Add(Activator.CreateInstance(handlerWrapperType, _serviceScopeFactory, handlerType, baseHandlerType) as IMessageHandlerWrapper);
            }
        }

        private Type GetHandlerWrapperImplementationType(Type baseHandlerType)
        {
            return typeof(MessageHandlerWrapper<>).MakeGenericType(baseHandlerType.GetGenericArguments().Single());
        }
    }
}
