using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus
{
    /// <summary>
    /// EventBus 根模块:注册基础设施,并在所有下游模块完成 ConfigureServices 后统一聚合 <see cref="EventBusOptions"/> 完成服务注册。
    /// </summary>
    /// <remarks>
    /// 采用两段式:<see cref="ConfigureServices"/> 只注册最小集,真正的 publisher / subscribe 推迟到 <see cref="PostConfigureServices"/>,
    /// 避免下游模块的 extension 因 options 提前解析而丢失。
    /// </remarks>
    public class CoreEventBusModule : CoreModuleBase
    {
        /// <summary>注册基础设施:HostedService 和默认 invoker。</summary>
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            // 这里只注册基础设施,publisher/subscribe 的注册推迟到 PostConfigureServices,
            // 等所有下游模块通过 services.Configure<EventBusOptions> 堆完 extension 再一次性 Configure
            context.Services.AddHostedService<EventBusBackgroundService>();
            context.Services.TryAddSingleton<IMessageHandlerInvoker, DefaultMessageHandlerInvoker>();
        }

        /// <summary>所有模块 ConfigureServices 已跑完,此时 options 已聚合,一次性把所有 extension 安装到 IoC。</summary>
        public override void PostConfigureServices(ServiceCollectionContext context)
        {
            // 直接走 IServiceCollection 上的 IConfigureOptions 描述符,不再 BuildServiceProvider():
            // - 避免 "Building service provider during configuration" 反模式与对应警告
            // - 避免在配置阶段实例化 Singleton 再 Dispose 引发副作用
            context.Services.ResolveAndConfigureEventBus();
        }
    }
}
