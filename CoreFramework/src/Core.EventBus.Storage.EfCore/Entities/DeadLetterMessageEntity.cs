using System;

namespace Core.EventBus.Storage.EfCore.Entities
{
    /// <summary>死信表的 EF 实体,承接超过 <c>OutboxOptions.MaxRetries</c> 仍失败的消息。</summary>
    /// <remarks>
    /// schema 与 outbox 几乎一致,多了 <see cref="DeadAtUtc"/> 表示转入死信的时刻;
    /// 保留原 <see cref="Id"/> 是为了运维侧用消息 id 追溯链路。
    /// <para>死信表只写不读 —— dispatcher 不再扫描。典型处置:人工排查后复制回 outbox 重投,或归档/删除。</para>
    /// </remarks>
    public class DeadLetterMessageEntity
    {
        /// <summary>死信表行主键(沿用 outbox 行原 <see cref="OutboxMessageEntity.Id"/>);便于按 outbox 链路追溯。</summary>
        public Guid Id { get; set; }
        /// <summary>业务消息 Id;沿用 outbox 行原 <see cref="OutboxMessageEntity.MessageId"/>。</summary>
        public Guid MessageId { get; set; }
        public int Version { get; set; }
        public string AssemblyName { get; set; }
        public string MessageName { get; set; }
        public string MessageData { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UtcTime { get; set; }
        public int RetryCount { get; set; }
        public string LastError { get; set; }

        /// <summary>从 outbox 转入死信的 UTC 时刻;按此加索引以便按时间段排查。</summary>
        public DateTime DeadAtUtc { get; set; }
    }
}
