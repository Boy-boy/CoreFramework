using Core.EventBus;
using Core.EventBus.Abstraction;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusServiceCollectionExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.TryAddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
            services.TryAddSingleton<IEventHandlerFactory, IocEventHandlerFactory>();
            return services;
        }

        public static IServiceCollection RegistrarIntegrationEventHandlers(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) return services;
            var typeInfos = assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => t.IsClass)
                .Where(t => t.IsPublic)
                .Where(t => !t.IsAbstract)
                .Where(t => !t.IsInterface)
                .ToList();
            foreach (var typeInfo in typeInfos)
            {
                var baseHandlerTypes = typeInfo.GetInterfaces().Where(t =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));
                foreach (var baseHandlerType in baseHandlerTypes)
                {
                    services.AddTransient(baseHandlerType, typeInfo);
                }
            }
            return services;
        }
    }
}
