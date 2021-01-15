using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Messaging.Impl
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

        public IEnumerable<IMessageHandlerWrapper> GetMessageHandlers(Type messageType)
        {
            throw new NotImplementedException();
        }

        public void Initialize(params Assembly[] assemblies)
        {
            if (assemblies == null) return;
            var handlerTypes = assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => typeof(IMessageHandler).GetTypeInfo().IsAssignableFrom(t))
                .ToList();
            foreach (var handlerType in handlerTypes)
            {
                RegisterHandler(handlerType);
            }
        }

        private void RegisterHandler(Type handlerType)
        {
            var baseHandlerTypes = handlerType
                .GetInterfaces()
                .Where(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
            foreach (var baseHandlerType in baseHandlerTypes)
            {
                var messageType = baseHandlerType.GenericTypeArguments.Single();

                if (!_handlerDict.TryGetValue(messageType, out var handlers))
                {
                    handlers = new List<IMessageHandlerWrapper>();
                    _handlerDict.Add(messageType, handlers);
                }
                var handlerWrapperType = GetHandlerWrapperImplementationType(baseHandlerType);
                handlers.Add(Activator.CreateInstance(handlerWrapperType, _serviceScopeFactory, baseHandlerType) as IMessageHandlerWrapper);
            }
        }

        private Type GetHandlerWrapperImplementationType(Type handlerInterfaceType)
        {
            return typeof(MessageHandlerWrapper<>).MakeGenericType(handlerInterfaceType.GetGenericArguments().Single());
        }
    }
}
