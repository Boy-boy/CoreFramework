using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus
{
    public class EventBusOptions
    {
        private Assembly[] _messageHandlerAssemblies;

        public EventBusOptions()
        {
            Extensions = new List<IEventBusOptionsExtensions>();
        }

        public Assembly[] MessageHandlerAssemblies => _messageHandlerAssemblies;

        public List<IEventBusOptionsExtensions> Extensions { get; set; }

        /// <summary>
        /// 注册消费者
        /// </summary>
        /// <param name="assemblies"></param>
        public void AddConsumers(params Assembly[] assemblies)
        {
            _messageHandlerAssemblies = assemblies;
        }
    }

    public static class EventBusOptionsExtensions
    {
        public static void AddExtensions(this EventBusOptions options, IEventBusOptionsExtensions eventBusOptionExtensions)
        {
            if (eventBusOptionExtensions == null)
                throw new AggregateException(nameof(eventBusOptionExtensions));
            options.Extensions.Add(eventBusOptionExtensions);
        }

        public static void Configure(this EventBusOptions options, IServiceCollection services)
        {
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }
            services.TryRegisterMessageHandlers(options.MessageHandlerAssemblies);
        }

        /// <summary>
        /// 向IOC容器注册Handler处理器
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        private static void TryRegisterMessageHandlers(this IServiceCollection services, Assembly[] assemblies)
        {
            if (assemblies == null) return;
            var handlerTypes = MessageHandlerExtensions.GetHandlerTypes(assemblies);
            foreach (var handlerType in handlerTypes)
            {
                var baseHandlerTypes = MessageHandlerExtensions.GetBaseHandlerTypes(handlerType);
                foreach (var baseHandlerType in baseHandlerTypes)
                {
                    services.TryAddTransient(baseHandlerType, handlerType);
                    services.TryAddTransient(handlerType);
                }
            }
        }
    }
}
