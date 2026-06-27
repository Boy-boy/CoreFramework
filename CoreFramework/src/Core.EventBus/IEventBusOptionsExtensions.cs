using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    /// <summary>
    /// EventBus 的"模块扩展点":各实现模块通过本接口把自己的服务注册到 IoC。
    /// </summary>
    /// <remarks>
    /// 实现挂到 <see cref="EventBusOptions.Extensions"/>,由框架在 <see cref="EventBusOptionsExtensions.Configure"/> 阶段统一回调 <see cref="AddServices"/>。
    /// </remarks>
    public interface IEventBusOptionsExtensions
    {
        /// <summary>向 IoC 注册本模块所需的服务(publisher / subscribe / handler manager 等)。</summary>
        void AddServices(IServiceCollection services);
    }
}
