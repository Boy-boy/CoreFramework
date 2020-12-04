using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.EventBus.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.EventBus
{
    public static class EventBusApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseEventBus(
            this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            var options = app.ApplicationServices.GetRequiredService<IOptions<EventBusOptions>>();
            AutoSubscribe(eventBus, options.Value.AutoRegistrarHandlersAssemblies);
            return app;
        }

        private static void AutoSubscribe(IEventBus eventBus, IEnumerable<Assembly> assemblies)
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
                    var eventType = baseHandlerType.GenericTypeArguments.FirstOrDefault();
                    var subscribeWrapperType = typeof(SubscribeWrapper<,>).MakeGenericType(eventType, handlerType);
                    var subscribeWrapper = Activator.CreateInstance(subscribeWrapperType, eventBus);
                    subscribeWrapperType.GetMethod("Subscribe")?.Invoke(subscribeWrapper, new object[] { });
                }
            }
        }

        public class SubscribeWrapper<T, TH>
             where T : IntegrationEvent
             where TH : IIntegrationEventHandler<T>, new()
        {
            private readonly IEventBus _eventBus;

            public SubscribeWrapper(IEventBus eventBus)
            {
                _eventBus = eventBus;
            }

            public void Subscribe()
            {
                _eventBus.Subscribe<T, TH>();
            }
        }
    }
}
