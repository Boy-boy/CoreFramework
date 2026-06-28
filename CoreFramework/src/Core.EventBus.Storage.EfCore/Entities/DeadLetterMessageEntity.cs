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

        /// <summary>载体格式版本号;与 outbox 行一致。</summary>
        public int Version { get; set; }

        /// <summary>消息 CLR 类型所在程序集短名;与 outbox 行一致,供运维侧重投时反射重建类型。</summary>
        public string AssemblyName { get; set; }

        /// <summary>消息 CLR 类型全名;与 outbox 行一致。</summary>
        public string MessageName { get; set; }

        /// <summary>原 payload(JSON);与 outbox 行一致,运维侧可直接复制回 outbox 重投。</summary>
        public string MessageData { get; set; }

        /// <summary>原写入 outbox 的本地时间;不参与排序。</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>原写入 outbox 的 UTC 时间;运维侧可借此还原消息的"生产时刻"。</summary>
        public DateTime UtcTime { get; set; }

        /// <summary>转入死信前的累计失败次数(含最后一次失败);通常 = <c>OutboxOptions.MaxRetries + 1</c>。</summary>
        public int RetryCount { get; set; }

        /// <summary>最后一次失败的异常摘要(配置限长 4000 字符)。</summary>
        public string LastError { get; set; }

        /// <summary>从 outbox 转入死信的 UTC 时刻;按此加索引以便按时间段排查。</summary>
        public DateTime DeadAtUtc { get; set; }
    }
}
