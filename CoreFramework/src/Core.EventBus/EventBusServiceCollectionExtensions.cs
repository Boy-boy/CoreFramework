using Core.EventBus;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Reflection;
using Core.EventBus.Transaction;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusServiceCollectionExtensions
    {
        public static EventBusBuilder AddEventBus(this IServiceCollection services, Action<EventBusOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.TryAddSingleton<IMessageHandlerManager, MessageHandlerManager>();
            services.TryAddSingleton<IMessageHandlerProvider, MessageHandlerProvider>();
            services.TryAddSingleton<ITransactionAccessor, TransactionAccessor>();
            services.AddHostedService<EventBusBackgroundService>();
            services.Configure(configureOptions);

            var options = new EventBusOptions();
            configureOptions.Invoke(options);
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }
            services.TryRegisterMessageHandlers(options.MessageHandlerAssemblies);
            return new EventBusBuilder(services);
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
