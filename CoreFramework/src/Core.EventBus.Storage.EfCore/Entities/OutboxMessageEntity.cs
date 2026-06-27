using Core.EventBus.Outbox;
using System;

namespace Core.EventBus.Storage.EfCore.Entities
{
    /// <summary>
    /// outbox 表的 EF 实体;字段与 <see cref="MessageEnvelope"/> 几乎一一对应,
    /// 额外的 nullable / 长度约束在 <c>OutboxMessageConfiguration</c> 中声明。
    /// </summary>
    /// <remarks>
    /// 不直接把 <see cref="MessageEnvelope"/> 映射为实体:Envelope 是跨层 DTO,不应被 EF 元数据污染;
    /// 实体的字段顺序 / 长度 / 可空性是 schema 决策,跟载体 DTO 解耦更好维护。
    /// </remarks>
    public class OutboxMessageEntity
    {
        /// <summary>outbox 表行主键;dispatcher 内部寻址使用。</summary>
        public Guid Id { get; set; }
        /// <summary>业务消息 Id;broker header MessageId + inbox 去重键。</summary>
        public Guid MessageId { get; set; }
        public int Version { get; set; }
        public string AssemblyName { get; set; }
        public string MessageName { get; set; }
        public string MessageData { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UtcTime { get; set; }

        public int RetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public string LastError { get; set; }

        /// <summary>由载体 DTO 构造实体(生产端写入)。</summary>
        public static OutboxMessageEntity FromEnvelope(MessageEnvelope m) => new()
        {
            Id = m.Id,
            MessageId = m.MessageId,
            Version = m.Version,
            AssemblyName = m.AssemblyName,
            MessageName = m.MessageName,
            MessageData = m.MessageData,
            CreateTime = m.CreateTime,
            UtcTime = m.UtcTime,
            RetryCount = m.RetryCount,
            NextRetryAt = m.NextRetryAt,
            LastError = m.LastError
        };

        /// <summary>把实体投影为载体 DTO(dispatcher 读取)。</summary>
        public MessageEnvelope ToEnvelope() => new()
        {
            Id = Id,
            MessageId = MessageId,
            Version = Version,
            AssemblyName = AssemblyName,
            MessageName = MessageName,
            MessageData = MessageData,
            CreateTime = CreateTime,
            UtcTime = UtcTime,
            RetryCount = RetryCount,
            NextRetryAt = NextRetryAt,
            LastError = LastError
        };
    }
}
