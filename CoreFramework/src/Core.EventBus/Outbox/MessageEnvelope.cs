using Core.Json.Newtonsoft;
using System;

namespace Core.EventBus.Outbox
{
    /// <summary>Outbox / 死信表的行级载体(消息信封);承载业务 payload + 元数据。</summary>
    /// <remarks>
    /// 不直接序列化 <see cref="IMessage"/>:storage 层需要的元数据(类型名/程序集名/重试信息/失败原因)不属于业务事件。
    /// 序列化用 Newtonsoft;dispatcher 端通过 AssemblyName + MessageName 重建 CLR 类型。
    /// </remarks>
    public class MessageEnvelope
    {
        /// <summary>仅供反序列化用,业务代码请使用带参构造。</summary>
        public MessageEnvelope()
        {
        }

        /// <summary>把一条业务事件包装成 outbox 行。</summary>
        /// <param name="aggregateRootEvent">实现了 <see cref="IMessage"/> 的领域/集成事件。</param>
        public MessageEnvelope(IMessage aggregateRootEvent)
        {
            Id = Guid.NewGuid();
            Version = 1;
            AssemblyName = aggregateRootEvent.GetType().Assembly.GetName().Name;
            MessageName = aggregateRootEvent.GetType().FullName;
            MessageData = aggregateRootEvent.ToJson();
            // CreateTime 同 UtcTime,仅为向后兼容字段顺序而保留
            var nowUtc = DateTime.UtcNow;
            CreateTime = nowUtc;
            UtcTime = nowUtc;
            RetryCount = 0;
            NextRetryAt = null;
            LastError = null;
        }

        /// <summary>outbox / inbox 关联的唯一 id;同时作为 broker MessageId,供 inbox 去重。</summary>
        public Guid Id { get; set; }

        /// <summary>载体格式版本号,当前固定为 1;未来调整字段语义时可借此分流。</summary>
        public int Version { get; set; }

        /// <summary>消息 CLR 类型所在程序集短名;dispatcher 端 Assembly.Load 后反射重建类型。</summary>
        public string AssemblyName { get; set; }

        /// <summary>消息 CLR 类型全名(<c>type.FullName</c>)。</summary>
        public string MessageName { get; set; }

        /// <summary>序列化后的 payload(Newtonsoft JSON);dispatcher 原样作为 broker 消息体写出。</summary>
        public string MessageData { get; set; }

        /// <summary>写入 outbox 的本地时间;仅供日志,不参与排序。</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>写入 outbox 的 UTC 时间;dispatcher 按此排序保证大致 FIFO。</summary>
        public DateTime UtcTime { get; set; }

        /// <summary>已尝试投递的次数;每次失败由 <see cref="IOutboxStorage.MarkFailedAsync"/> 递增。</summary>
        public int RetryCount { get; set; }

        /// <summary>下次允许投递时刻(UTC);<c>null</c> 表示立即可投递。</summary>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>上次投递失败的异常摘要(截断到 4000 字符)。</summary>
        public string LastError { get; set; }
    }
}
