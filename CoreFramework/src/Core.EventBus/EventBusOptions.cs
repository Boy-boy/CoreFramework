using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus
{
    /// <summary>
    /// EventBus 的统一配置入口:承载待扫描 handler 程序集与下游模块的扩展。
    /// </summary>
    public class EventBusOptions
    {
        private readonly List<Assembly> _messageHandlerAssemblies = new();

        public EventBusOptions()
        {
            Extensions = new List<IEventBusOptionsExtensions>();
        }

        /// <summary>待扫描 <see cref="IMessageHandler"/> 实现类型的程序集集合(已去重)。</summary>
        public Assembly[] MessageHandlerAssemblies => _messageHandlerAssemblies.ToArray();

        /// <summary>下游模块提供的扩展集合,每个扩展负责注册自己模块的服务。</summary>
        public List<IEventBusOptionsExtensions> Extensions { get; set; }

        /// <summary>追加待扫描 handler 的 assembly;多次调用累加并去重。</summary>
        public void AddConsumers(params Assembly[] assemblies)
        {
            if (assemblies == null) return;
            foreach (var assembly in assemblies)
            {
                if (assembly != null && !_messageHandlerAssemblies.Contains(assembly))
                {
                    _messageHandlerAssemblies.Add(assembly);
                }
            }
        }
    }

    /// <summary>
    /// <see cref="EventBusOptions"/> 的扩展方法集合。
    /// </summary>
    public static class EventBusOptionsExtensions
    {
        /// <summary>把一个 <see cref="IEventBusOptionsExtensions"/> 追加到 options 上。</summary>
        public static void AddExtensions(this EventBusOptions options, IEventBusOptionsExtensions eventBusOptionExtensions)
        {
            if (eventBusOptionExtensions == null)
                throw new ArgumentNullException(nameof(eventBusOptionExtensions));
            options.Extensions.Add(eventBusOptionExtensions);
        }

        /// <summary>一次性应用所有扩展,并把 handler 程序集中的 <see cref="IMessageHandler"/> 实现注册到 IoC。</summary>
        public static void Configure(this EventBusOptions options, IServiceCollection services)
        {
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }
            services.TryRegisterMessageHandlers(options.MessageHandlerAssemblies);
        }

        /// <summary>把 handler 类按"每个泛型接口"和"具体类型"两种方式各注册一次到 IoC。</summary>
        /// <remarks>
        /// 注册到 <c>IMessageHandler&lt;T&gt;</c> 供框架按消息类型解析;注册到具体类型供 invoker 在 scope 内按 handlerType 直接 resolve。
        /// </remarks>
        private static void TryRegisterMessageHandlers(this IServiceCollection services, Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0) return;
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
