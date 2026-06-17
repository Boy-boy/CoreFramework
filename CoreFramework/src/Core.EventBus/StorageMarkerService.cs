namespace Core.EventBus
{
    /// <summary>
    /// "项目已启用 outbox / inbox 存储"的 DI 标记类。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 由 <c>options.AddEfCoreEventBusStorage&lt;TDbContext&gt;()</c> 注册为 Singleton；
    /// 其他模块（典型为 UoW 集成层）通过 <c>sp.GetService&lt;StorageMarkerService&gt;()</c>
    /// 是否为 null 来判断"用户是否启用了 outbox 存储"，据此决定是否暴露 outbox 上下文 / 包装 UoW。
    /// </para>
    /// <para>
    /// 之所以用单独的 marker 类而不直接判断 <see cref="Outbox.IOutboxStorage"/> 是否注册：
    /// 是为了避免触发 Scoped 服务的 root-provider 解析告警 —— marker 是 Singleton，
    /// 可以从任意 scope 安全查询。
    /// </para>
    /// </remarks>
    public class StorageMarkerService
    {
    }
}
