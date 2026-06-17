namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地事件 handler 注册表实现。直接继承通用 <see cref="MessageHandlerManager"/>，
    /// 仅充当类型隔离的标记 —— 让 DI 容器把"本地事件订阅表"和"集成事件订阅表"区分开。
    /// </summary>
    public class LocalMessageHandlerManager : MessageHandlerManager, ILocalMessageHandlerManager
    {
    }
}
