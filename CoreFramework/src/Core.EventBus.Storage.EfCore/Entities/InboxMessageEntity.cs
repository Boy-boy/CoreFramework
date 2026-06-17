using Core.EventBus.Outbox;
using System;

namespace Core.EventBus.Storage.EfCore.Entities
{
    /// <summary>
    /// inbox 表的 EF 实体，用于消费端"消息 × handler"维度的幂等去重。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 表使用 <c>(MessageId, ConsumerGroup)</c> <b>复合主键</b>，约束语义：
    /// </para>
    /// <list type="bullet">
    ///   <item><description>同一 <c>MessageId</c> 被同一 handler 处理过 → 主键冲突 → 第二次 acquire 返回 false；</description></item>
    ///   <item><description>同一 <c>MessageId</c> 被两个不同 handler 订阅 → 两条不同主键的行，互不影响；</description></item>
    ///   <item><description>消费回滚（业务失败） → inbox 行随事务回滚 → 下次重投可正常处理。</description></item>
    /// </list>
    /// </remarks>
    public class InboxMessageEntity
    {
        /// <summary>来自 <see cref="MessageEnvelope.Id"/> 的全局消息 id。</summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// 消费方标识。框架默认填入 handler 类型的 <c>FullName</c>，
        /// 这样多 handler 订阅同一消息时各自有独立的去重记录。
        /// </summary>
        public string ConsumerGroup { get; set; }

        /// <summary>本条消息被该 handler 处理（写入 inbox）的 UTC 时刻。</summary>
        /// <remarks>由 <c>InboxCleanupService</c> 按此字段判断是否超过保留期。</remarks>
        public DateTime ProcessedAtUtc { get; set; }
    }
}
