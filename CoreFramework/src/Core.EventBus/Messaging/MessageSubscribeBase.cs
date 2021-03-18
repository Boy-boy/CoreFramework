using System;
using System.Linq;
using System.Reflection;

namespace Core.EventBus.Messaging
{
    public abstract class MessageSubscribeBase : IMessageSubscribe
    {
        public void Initialize(params Assembly[] assemblies)
        {
            var handlerTypes = MessageHandlerExtensions.GetHandlerTypes(assemblies);
            foreach (var handlerType in handlerTypes)
            {
                var baseHandlerTypes = MessageHandlerExtensions.GetBaseHandlerTypes(handlerType);
                foreach (var baseHandlerType in baseHandlerTypes)
                {
                    var messageType = baseHandlerType.GenericTypeArguments.Single();
                    Subscribe(messageType, handlerType);
                }
            }
        }

        protected abstract void Subscribe(Type messageType, Type handlerType);

        public abstract void Subscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>, new();

        public abstract void UnSubscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>, new();

    }
}
