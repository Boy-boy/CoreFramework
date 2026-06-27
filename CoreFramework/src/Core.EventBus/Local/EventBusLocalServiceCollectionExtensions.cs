namespace Core.EventBus.Local
{
    /// <summary>提供 <c>EventBusOptions.AddLocalMq()</c> 流式扩展,供非模块化场景使用。</summary>
    public static class EventBusLocalServiceCollectionExtensions
    {
        /// <summary>把本地事件总线扩展挂到 options;由模块或 AddEventBus 统一应用。</summary>
        public static EventBusOptions AddLocalMq(this EventBusOptions options)
        {
            options.AddExtensions(new EventBusOptionsExtensions());
            return options;
        }
    }
}
