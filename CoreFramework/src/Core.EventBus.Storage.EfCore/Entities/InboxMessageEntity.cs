using Core.EventBus.Outbox;
using System;

namespace Core.EventBus.Storage.EfCore.Entities
{
    /// <summary>inbox 表的 EF 实体,用于"消息 × handler"维度的幂等去重。</summary>
    /// <remarks>
    /// <c>(MessageId, ConsumerGroup)</c> 复合主键:
    /// <list type="bullet">
    ///   <item><description>同一 MessageId 被同一 handler 处理过 → 主键冲突 → 第二次 acquire 返回 false</description></item>
    ///   <item><description>同一 MessageId 被两个 handler 订阅 → 两条不同主键的行,互不影响</description></item>
    ///   <item><description>消费回滚 → inbox 行随事务回滚 → 下次重投可正常处理</description></item>
    /// </list>
    /// </remarks>
    public class InboxMessageEntity
    {
        /// <summary>来自 <see cref="MessageEnvelope.Id"/> 的全局消息 id。</summary>
        public Guid MessageId { get; set; }

        /// <summary>消费方标识;框架默认填 handler 类型的 FullName,多 handler 订阅时各自独立去重。</summary>
        public string ConsumerGroup { get; set; }

        /// <summary>本条消息被该 handler 写入 inbox 的 UTC 时刻;<c>InboxCleanupService</c> 按此判断保留期。</summary>
        public DateTime ProcessedAtUtc { get; set; }
    }
}
