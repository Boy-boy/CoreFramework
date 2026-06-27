using System;

namespace Core.EventBus.Inbox
{
    /// <summary>消费端 inbox 的运行参数;配合 <see cref="IInboxStorage"/> 实现"每条消息每个 handler 最多处理一次"。</summary>
    /// <remarks>
    /// 保留期必须 &gt; broker 可能的最大重投延迟,否则边界场景会出现"清理后又收到重投 → 重复处理"。
    /// </remarks>
    public class InboxOptions
    {
        /// <summary>inbox 记录保留天数(默认 14);<c>ProcessedAtUtc</c> 早于阈值的行会被清理服务删除。</summary>
        public int RetentionDays { get; set; } = 14;

        /// <summary>启动时自动调用 <see cref="IInboxStorage.InitializeAsync"/> 建表;默认 true,用 EF Migrations 时可关。</summary>
        public bool AutoInitialize { get; set; } = true;

        /// <summary>两次清理之间的间隔(默认 1 小时);窗口以天计,不宜过短或过长。</summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    }
}
