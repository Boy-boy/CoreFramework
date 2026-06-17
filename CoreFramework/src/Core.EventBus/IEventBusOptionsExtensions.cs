using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    /// <summary>
    /// EventBus 的"模块扩展点"。每个 broker / 实现模块通过实现本接口把"自己模块需要的服务"
    /// 挂到 <see cref="EventBusOptions.Extensions"/> 列表里，由框架在 <see cref="EventBusOptionsExtensions.Configure"/>
    /// 阶段统一调用 <see cref="AddServices"/> 完成 IoC 注册。
    /// </summary>
    /// <remarks>
    /// 典型实现：<c>Core.EventBus.Local.EventBusOptionsExtensions</c>、
    /// <c>Core.EventBus.RabbitMQ.EventBusOptionsExtensions</c>。
    /// </remarks>
    public interface IEventBusOptionsExtensions
    {
        /// <summary>
        /// 向 IoC 注册本模块所需的服务（publisher / subscribe / handler manager 等）。
        /// </summary>
        void AddServices(IServiceCollection services);
    }
}
