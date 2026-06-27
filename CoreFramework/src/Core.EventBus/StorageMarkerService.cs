namespace Core.EventBus
{
    /// <summary>
    /// "项目已启用 outbox / inbox 存储"的 DI 标记类。
    /// </summary>
    /// <remarks>
    /// 由存储模块注册为 Singleton;其他模块通过查询本服务是否注册来判断是否需要包装 UoW / 暴露 outbox 上下文。
    /// 用单独 marker 类而非直接判断 <see cref="Outbox.IOutboxStorage"/> 是否注册,是为了避免触发 Scoped 服务的 root-provider 解析告警。
    /// </remarks>
    public class StorageMarkerService
    {
    }
}
