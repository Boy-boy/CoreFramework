using System;

namespace Core.EventBus.Inbox
{
    /// <summary>
    /// 消费端 inbox（接收簿）的运行参数。配合 <see cref="IInboxStorage"/> 实现"每条消息每个 handler
    /// 最多被处理一次"的语义。
    /// </summary>
    /// <remarks>
    /// inbox 表会随时间无限增长，因此需要后台清理服务（<c>InboxCleanupService</c>）周期删除
    /// 超过保留期的行。保留期不能比 broker 可能的最大重投延迟更短，否则一条"延迟很久才被重投"
    /// 的消息可能在 inbox 已清理后被错误地当作新消息再次处理。
    /// </remarks>
    public class InboxOptions
    {
        /// <summary>
        /// inbox 记录保留天数，超过 <c>ProcessedAtUtc &lt; UTC.Now - RetentionDays</c> 的行会被
        /// <c>InboxCleanupService</c> 删除；默认 14 天。
        /// </summary>
        /// <remarks>
        /// 取值要大于 broker 端可能的最大重投延迟（含 DLX/TTL 死信流转链路），否则会在边界
        /// 出现"清理后又收到重投 → 重复处理"的真空。
        /// </remarks>
        public int RetentionDays { get; set; } = 14;

        /// <summary>
        /// 启动时自动调用 <see cref="IInboxStorage.InitializeAsync"/> 建表；默认 true。
        /// 使用 EF Migrations 管理 schema 时建议关闭。
        /// </summary>
        public bool AutoInitialize { get; set; } = true;

        /// <summary>
        /// <c>InboxCleanupService</c> 两次清理之间的间隔；默认 1 小时。
        /// 太短没必要（保留窗口以天计），太长会让 inbox 表瞬时膨胀。
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    }
}
