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
        public static EventBusBuilder AddEventBus(this IServiceCollection services, Action<EventBusOptions> options = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.TryAddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
            services.TryAddSingleton<IEventHandlerFactory, IocEventHandlerFactory>();
            if (options != null)
            {
                services.Configure(options);
            }
            var builder = new EventBusBuilder(services);
            return builder;
        }

        public static EventBusBuilder RegistrarIntegrationEventHandlers(this EventBusBuilder builder, IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) return builder;
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
                    builder.Services.AddTransient(baseHandlerType, typeInfo);
                }
            }
            return builder;
        }
    }
}
