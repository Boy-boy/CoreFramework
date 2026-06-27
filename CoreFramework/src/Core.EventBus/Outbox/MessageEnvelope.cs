using Core.Json.Newtonsoft;
using System;

namespace Core.EventBus.Outbox
{
    /// <summary>
    /// Outbox / 死信表的 <b>行级载体</b>（消息信封）。同时充当：
    /// <list type="bullet">
    ///   <item><description>生产端：把 <see cref="IMessage"/> 序列化后塞进 outbox 表</description></item>
    ///   <item><description>Dispatcher：从 outbox 表反序列化回业务事件并投递到 broker</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// 之所以不直接序列化 <see cref="IMessage"/>，是因为我们需要在 storage 层关心元数据
    /// （类型名、程序集名、重试信息、失败原因），而这些信息不属于业务事件本身。
    /// 序列化使用 Newtonsoft（与现有 publisher 一致），反序列化在 dispatcher 端
    /// 通过 AssemblyName + MessageName 重建 CLR 类型再 ToJson 还原。
    /// </remarks>
    public class MessageEnvelope
    {
        /// <summary>仅供反序列化用，业务代码请使用带参构造。</summary>
        public MessageEnvelope()
        {
        }

        /// <summary>
        /// 把一条业务事件包装成 outbox 行。
        /// </summary>
        /// <param name="aggregateRootEvent">实现了 <see cref="IMessage"/> 的领域/集成事件。</param>
        public MessageEnvelope(IMessage aggregateRootEvent)
        {
            Id = Guid.NewGuid();
            Version = 1;
            AssemblyName = aggregateRootEvent.GetType().Assembly.GetName().Name;
            MessageName = aggregateRootEvent.GetType().FullName;
            MessageData = aggregateRootEvent.ToJson();
            // 统一用 UTC,避免跨时区/DST 切换日志混淆。CreateTime 保留是为向后兼容字段顺序
            var nowUtc = DateTime.UtcNow;
            CreateTime = nowUtc;
            UtcTime = nowUtc;
            RetryCount = 0;
            NextRetryAt = null;
            LastError = null;
        }

        /// <summary>
        /// outbox / inbox 关联的唯一 id。同时作为 broker 消息的 MessageId，
        /// 用于 inbox 去重（同一 Id 在 <c>(MessageId, ConsumerGroup)</c> 复合主键下只接受一次）。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 载体格式版本号。当前固定为 1；未来如调整字段语义可借此分流。
        /// </summary>
        public int Version { get; set; }

        /// <summary>消息 CLR 类型所在程序集的短名（用于 dispatcher 端 <c>Assembly.Load</c> 后反射重建类型）。</summary>
        public string AssemblyName { get; set; }

        /// <summary>消息 CLR 类型全名（即 <c>type.FullName</c>）。</summary>
        public string MessageName { get; set; }

        /// <summary>
        /// 序列化后的事件 payload（Newtonsoft JSON）。
        /// Dispatcher 投递时把它原样作为 broker 消息体写出，不再二次反序列化为对象。
        /// </summary>
        public string MessageData { get; set; }

        /// <summary>事件被写入 outbox 的本地时间。仅供日志/排查可读，不参与排序。</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>事件被写入 outbox 的 UTC 时间。dispatcher 按此排序保证大致 FIFO。</summary>
        public DateTime UtcTime { get; set; }

        /// <summary>已尝试投递的次数。每次失败由 <see cref="IOutboxStorage.MarkFailedAsync"/> 递增。</summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 下次允许投递时刻（UTC）。
        /// <list type="bullet">
        ///   <item><description><c>null</c>：立即可投递（首次写入或重试时间已过）</description></item>
        ///   <item><description>有值：dispatcher 必须等到此时间之后才能再次拉取这条消息</description></item>
        /// </list>
        /// </summary>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>
        /// 上次投递失败的异常摘要（已截断到 4000 字符避免列爆炸）。
        /// 用于排查反复失败的消息。
        /// </summary>
        public string LastError { get; set; }
    }
}
