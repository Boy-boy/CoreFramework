using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Core.EventBus
{
    public class MessageHandlerExtensions
    {
        public static IEnumerable<Type> GetHandlerTypes(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                return new List<Type>();
            return assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => typeof(IMessageHandler).GetTypeInfo().IsAssignableFrom(t))
                .ToList();
        }

        public static IEnumerable<Type> GetBaseHandlerTypes(Type handlerType)
        {
            var baseHandlerTypes = handlerType
                .GetInterfaces()
                .Where(t =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
            baseHandlerTypes = baseHandlerTypes.Distinct();
            return baseHandlerTypes;
        }
    }
}
