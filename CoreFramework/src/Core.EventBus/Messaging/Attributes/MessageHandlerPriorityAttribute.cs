using System;
using System.Linq;

namespace Core.EventBus
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class MessageHandlerPriorityAttribute : Attribute
    {
        public virtual int Priority { get; }

        public MessageHandlerPriorityAttribute()
        : this(0)
        {
        }

        public MessageHandlerPriorityAttribute(int priority)
        {
            Priority = priority;
        }

        public static int GetPriority(Type messageType, Type handlerType)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }
            if (handlerType == null)
            {
                throw new ArgumentNullException(nameof(handlerType));
            }
            var handleMethods = handlerType
                .GetMethods()
                .Where(x => x.Name == "HandleAsync");
            foreach (var method in handleMethods)
            {
                var methodParameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
                if (methodParameterTypes.Length != 1 || methodParameterTypes[0] != messageType) continue;
                var methodPriorityAttributes = method.GetCustomAttributes(true).OfType<MessageHandlerPriorityAttribute>().ToList();
                if (methodPriorityAttributes.Any())
                {
                    return methodPriorityAttributes.First().Priority;
                }
            }
            return handlerType.GetCustomAttributes(true).OfType<MessageHandlerPriorityAttribute>().FirstOrDefault()?.Priority ?? 0;
        }
    }
}
