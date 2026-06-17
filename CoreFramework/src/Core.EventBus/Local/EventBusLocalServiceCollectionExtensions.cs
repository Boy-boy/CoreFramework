namespace Core.EventBus.Local
{
    /// <summary>
    /// 提供 <c>EventBusOptions.AddLocalMq()</c> 流式扩展，方便非模块化场景注册本地事件总线。
    /// </summary>
    public static class EventBusLocalServiceCollectionExtensions
    {
        /// <summary>
        /// 把本地事件总线扩展（<see cref="EventBusOptionsExtensions"/>）挂到 options。
        /// 由 <see cref="CoreEventBusModule.PostConfigureServices"/> 或
        /// <see cref="Microsoft.Extensions.DependencyInjection.EventBusServiceCollectionExtensions.AddEventBus"/>
        /// 统一应用。
        /// </summary>
        public static EventBusOptions AddLocalMq(this EventBusOptions options)
        {
            options.AddExtensions(new EventBusOptionsExtensions());
            return options;
        }
    }
}
