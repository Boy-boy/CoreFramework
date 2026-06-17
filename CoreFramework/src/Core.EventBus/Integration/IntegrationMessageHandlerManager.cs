namespace Core.EventBus.Integration
{
    /// <summary>
    /// 集成事件 handler 注册表的默认实现。直接继承通用 <see cref="MessageHandlerManager"/>，
    /// 仅作为类型隔离的标记 —— 让 DI 把集成事件的订阅表与本地事件的订阅表区分开。
    /// </summary>
    public class IntegrationMessageHandlerManager : MessageHandlerManager, IIntegrationMessageHandlerManager
    {
    }
}
