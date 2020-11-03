using Core.EventBus;
using Core.EventBus.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusServiceCollectionExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptions> action = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
            services.AddSingleton<IEventHandlerFactory, IocEventHandlerFactory>();
            if (action == null) return services;
            services.Configure(action);
            var option = new EventBusOptions();
            action(option);
            AutoRegistrarHandlers(services, option.AutoRegistrarHandlersAssemblies);
            return services;
        }

        public static void AutoRegistrarHandlers(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) return;
            var handlerTypes = assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => typeof(IIntegrationEventHandler).GetTypeInfo().IsAssignableFrom(t))
                .ToList();
            foreach (var handlerType in handlerTypes)
            {
                var baseHandlerTypes = handlerType.GetInterfaces().Where(t =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));
                foreach (var baseHandlerType in baseHandlerTypes)
                {
                    services.TryAddTransient(baseHandlerType, handlerType);
                }
            }
        }
    }
}
