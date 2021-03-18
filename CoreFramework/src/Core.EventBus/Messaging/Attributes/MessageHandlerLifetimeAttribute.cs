using System;
using System.Linq;

namespace Core.EventBus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageHandlerLifetimeAttribute : Attribute
    {
        public MessageHandlerLifetime Lifetime { get; }

        public MessageHandlerLifetimeAttribute(MessageHandlerLifetime lifetime)
        {
            Lifetime = lifetime;
        }

        public static MessageHandlerLifetime GetHandlerLifetime(Type handlerType)
        {
            if (handlerType == null)
            {
                throw new ArgumentNullException(nameof(handlerType));
            }
            return handlerType
                       .GetCustomAttributes(true)
                       .OfType<MessageHandlerLifetimeAttribute>()
                       .FirstOrDefault()
                       ?.Lifetime
                   ?? MessageHandlerLifetime.Transient;
        }
    }
}
