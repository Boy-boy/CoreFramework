using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Local
{
    /// <summary>本地事件总线模块;引入后可注入 <see cref="ILocalPublisher"/> 进程内派发事件。</summary>
    /// <remarks>
    /// 真正的服务注册延后到 <see cref="CoreEventBusModule.PostConfigureServices"/>:
    /// 这里只把 <see cref="EventBusOptionsExtensions"/> 挂到 options.Extensions。
    /// </remarks>
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusLocalModule : CoreModuleBase
    {
        /// <summary>把 LocalMq 扩展挂到 <see cref="EventBusOptions"/>。</summary>
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddLocalMq();
            });
        }
    }
}
