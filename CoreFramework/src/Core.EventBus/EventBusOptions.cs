using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus
{
    /// <summary>
    /// EventBus 的统一配置入口。承载两类信息：
    /// <list type="bullet">
    ///   <item><description><see cref="MessageHandlerAssemblies"/>：待扫描 handler 的程序集，
    ///   由启动期自动反射注册到 IoC，由 <see cref="EventBusBackgroundService"/> 注入到各 subscribe。</description></item>
    ///   <item><description><see cref="Extensions"/>：下游模块（如 Local / RabbitMQ）通过
    ///   <see cref="IEventBusOptionsExtensions"/> 把"自己需要的服务"挂上来，
    ///   由 <see cref="CoreEventBusModule.PostConfigureServices"/> 统一调用 AddServices 一次性注册。</description></item>
    /// </list>
    /// </summary>
    public class EventBusOptions
    {
        private readonly List<Assembly> _messageHandlerAssemblies = new();

        public EventBusOptions()
        {
            Extensions = new List<IEventBusOptionsExtensions>();
        }

        /// <summary>
        /// 待扫描 <see cref="IMessageHandler"/> 实现类型的程序集集合（已去重）。
        /// 启动期由 <see cref="MessageHandlerExtensions.GetHandlerTypes"/> 反射扫描。
        /// </summary>
        public Assembly[] MessageHandlerAssemblies => _messageHandlerAssemblies.ToArray();

        /// <summary>
        /// 下游模块提供的扩展集合。每个扩展负责向 IoC 注册自己模块需要的服务
        /// （publisher、subscribe、handler manager 等）。
        /// </summary>
        public List<IEventBusOptionsExtensions> Extensions { get; set; }

        /// <summary>
        /// 注册待扫描 handler 的 assembly。多次调用累加（去重）。
        /// </summary>
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
    /// <see cref="EventBusOptions"/> 的扩展方法集合。供下游模块 / 应用启动代码调用。
    /// </summary>
    public static class EventBusOptionsExtensions
    {
        /// <summary>
        /// 把一个 <see cref="IEventBusOptionsExtensions"/> 追加到 options 上。
        /// 框架在 <see cref="Configure"/> 阶段会逐一调用 <see cref="IEventBusOptionsExtensions.AddServices"/>。
        /// </summary>
        public static void AddExtensions(this EventBusOptions options, IEventBusOptionsExtensions eventBusOptionExtensions)
        {
            if (eventBusOptionExtensions == null)
                throw new ArgumentNullException(nameof(eventBusOptionExtensions));
            options.Extensions.Add(eventBusOptionExtensions);
        }

        /// <summary>
        /// 一次性应用所有扩展，并把 handler 程序集中的 <see cref="IMessageHandler"/> 实现注册到 IoC。
        /// 由 <see cref="CoreEventBusModule.PostConfigureServices"/> 调用。
        /// </summary>
        public static void Configure(this EventBusOptions options, IServiceCollection services)
        {
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }
            services.TryRegisterMessageHandlers(options.MessageHandlerAssemblies);
        }

        /// <summary>
        /// 向 IoC 容器注册 Handler 处理器。
        /// </summary>
        /// <remarks>
        /// 一个 handler 类可以同时实现多个 <c>IMessageHandler&lt;T&gt;</c>，
        /// 这里把它按"每个泛型接口"和"具体类型"两种方式各注册一次：
        /// <list type="bullet">
        ///   <item><description>注册到 <c>IMessageHandler&lt;T&gt;</c>：供框架按消息类型解析</description></item>
        ///   <item><description>注册到具体类型：供 invoker 在 scope 内按 handlerType 直接 resolve</description></item>
        /// </list>
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
