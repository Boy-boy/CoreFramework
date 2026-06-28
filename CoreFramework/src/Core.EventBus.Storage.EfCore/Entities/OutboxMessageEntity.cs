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

        /// <summary>载体格式版本号(当前固定为 1);未来调整字段语义时可借此分流。</summary>
        public int Version { get; set; }

        /// <summary>消息 CLR 类型所在程序集短名;dispatcher 端按此 <c>Assembly.Load</c> 反射重建类型。</summary>
        public string AssemblyName { get; set; }

        /// <summary>消息 CLR 类型全名(<c>Type.FullName</c>)。</summary>
        public string MessageName { get; set; }

        /// <summary>序列化后的 payload(JSON);dispatcher 原样作为 broker 消息体写出。</summary>
        public string MessageData { get; set; }

        /// <summary>写入 outbox 的本地时间;仅供日志,不参与排序。</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>写入 outbox 的 UTC 时间;dispatcher 按此排序保证大致 FIFO。</summary>
        public DateTime UtcTime { get; set; }

        /// <summary>已尝试投递的次数;每次失败由 <c>MarkFailedAsync</c> 递增。</summary>
        public int RetryCount { get; set; }

        /// <summary>下次允许投递时刻(UTC);<c>null</c> 表示立即可投递。</summary>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>上次投递失败的异常摘要(配置限长 4000 字符)。</summary>
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
