using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Core.EventBus
{
    /// <summary>
    /// EventBus 的统一配置入口:承载待扫描 handler 程序集与下游模块的扩展。
    /// </summary>
    public class EventBusOptions
    {
        private readonly List<Assembly> _messageHandlerAssemblies = new();
        private readonly List<IEventBusOptionsExtensions> _extensions = new();

        /// <summary>待扫描 <see cref="IMessageHandler"/> 实现类型的程序集集合(已去重)。</summary>
        public IReadOnlyList<Assembly> MessageHandlerAssemblies => _messageHandlerAssemblies;

        /// <summary>下游模块挂载的扩展集合(只读)。</summary>
        public IReadOnlyList<IEventBusOptionsExtensions> Extensions => _extensions;

        /// <summary>追加一个扩展;同实例只追加一次。</summary>
        internal void AddExtensionInternal(IEventBusOptionsExtensions extension)
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            _extensions.Add(extension);
        }

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
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            options.AddExtensionInternal(eventBusOptionExtensions);
        }

        /// <summary>一次性应用所有扩展,并把 handler 程序集中的 <see cref="IMessageHandler"/> 实现注册到 IoC。</summary>
        public static void Configure(this EventBusOptions options, IServiceCollection services)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (services == null) throw new ArgumentNullException(nameof(services));
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }
            services.TryRegisterMessageHandlers(options.MessageHandlerAssemblies);
        }

        /// <summary>
        /// 在不 BuildServiceProvider 的前提下聚合所有 <see cref="IConfigureOptions{EventBusOptions}"/>,
        /// 一次性把 extension 应用到 IoC。供 CoreEventBusModule / AddEventBus 调用。
        /// </summary>
        /// <remarks>
        /// <para><c>services.Configure&lt;EventBusOptions&gt;(Action&lt;EventBusOptions&gt;)</c> 内部以
        /// <c>AddSingleton&lt;IConfigureOptions&lt;EventBusOptions&gt;&gt;(ConfigureNamedOptions)</c> 的形式落到
        /// 描述符集合,因此直接遍历描述符即可拿到所有 configure 回调,无需 BuildServiceProvider 实例化容器。</para>
        /// <para>限制:本方法只识别 <c>ImplementationInstance</c> 注册形态(覆盖 <c>services.Configure</c> 与
        /// <c>services.AddSingleton</c> 二者)。若有人通过工厂或类型注册
        /// <see cref="IConfigureOptions{EventBusOptions}"/>,这些 configure 不会被采集 —— 但 EventBus 体系本身只用
        /// Action 形态,本仓库内的调用方都满足前提。</para>
        /// <para>运行期 <c>IOptions&lt;EventBusOptions&gt;</c> 走标准 OptionsManager,自动回放同一批
        /// IConfigureOptions,值与此处用作 setup 的 options 等价。</para>
        /// </remarks>
        public static void ResolveAndConfigureEventBus(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var options = new EventBusOptions();
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType != typeof(IConfigureOptions<EventBusOptions>))
                    continue;
                if (descriptor.ImplementationInstance is IConfigureOptions<EventBusOptions> cfg)
                {
                    cfg.Configure(options);
                }
            }

            options.Configure(services);
        }

        /// <summary>把 handler 类按"每个泛型接口"和"具体类型"两种方式各注册一次到 IoC。</summary>
        /// <remarks>
        /// 注册到 <c>IMessageHandler&lt;T&gt;</c> 供框架按消息类型解析;注册到具体类型供 invoker 在 scope 内按 handlerType 直接 resolve。
        /// </remarks>
        private static void TryRegisterMessageHandlers(this IServiceCollection services, IReadOnlyList<Assembly> assemblies)
        {
            if (assemblies == null || assemblies.Count == 0) return;
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
