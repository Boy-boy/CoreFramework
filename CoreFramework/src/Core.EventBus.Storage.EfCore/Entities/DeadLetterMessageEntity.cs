using System;

namespace Core.EventBus.Storage.EfCore.Entities
{
    /// <summary>
    /// 死信表的 EF 实体。承接超过 <c>OutboxOptions.MaxRetries</c> 仍然失败的消息。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 死信表与 outbox 表 schema 几乎一致，多了 <see cref="DeadAtUtc"/> 表示"转入死信的时刻"。
    /// 保留原 <see cref="Id"/> 是为了让运维侧能用消息 id 追溯链路（broker 日志、上游业务等）。
    /// </para>
    /// <para>
    /// 死信表只写不读 —— dispatcher 不会再扫描它。处置方式：
    /// <list type="bullet">
    ///   <item><description>人工排查并修复消费端 / 数据问题后，把行复制回 outbox 表重投；</description></item>
    ///   <item><description>确认无需再处理后归档或删除。</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class DeadLetterMessageEntity
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public string AssemblyName { get; set; }
        public string MessageName { get; set; }
        public string MessageData { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UtcTime { get; set; }
        public int RetryCount { get; set; }
        public string LastError { get; set; }

        /// <summary>消息从 outbox 转入死信的 UTC 时刻；按此字段加索引方便按时间段排查。</summary>
        public DateTime DeadAtUtc { get; set; }
    }
}
