using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地事件总线模块。引入本模块后业务侧即可注入 <see cref="ILocalPublisher"/> 在
    /// 进程内同步派发事件，handler 由 <see cref="IMessageHandlerInvoker"/> 按 DI scope 调用。
    /// </summary>
    /// <remarks>
    /// 依赖 <see cref="CoreEventBusModule"/> 提供基础设施。
    /// 注册流程：通过 <see cref="EventBusOptions"/> 的扩展机制把 <see cref="EventBusOptionsExtensions"/>
    /// 挂到 options.Extensions，待 <see cref="CoreEventBusModule.PostConfigureServices"/> 统一应用。
    /// </remarks>
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusLocalModule : CoreModuleBase
    {
        /// <summary>
        /// 把 LocalMq 扩展挂到 <see cref="EventBusOptions"/>。
        /// 真正的 publisher / subscribe 注册延后到 PostConfigureServices。
        /// </summary>
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddLocalMq();
            });
        }
    }
}
