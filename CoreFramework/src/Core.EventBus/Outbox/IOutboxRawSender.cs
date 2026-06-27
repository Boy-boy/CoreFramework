using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Outbox
{
    /// <summary>绕过 outbox 的原始投递接口;<b>仅供 dispatcher 内部使用</b>。</summary>
    /// <remarks>
    /// 独立出此接口的原因:dispatcher 若复用 PublishAsync 会因检测到 outbox 上下文再次写表,形成死循环。
    /// 实现约束:payload 原样透传(不再加工/序列化);broker 临时不可达可做小范围 Polly 重试,大范围重试交给 dispatcher 退避。
    /// </remarks>
    public interface IOutboxRawSender
    {
        /// <summary>将 outbox 中的消息原样发送到 broker。</summary>
        /// <param name="message">已从 outbox 表读出的消息载体(payload 为 JSON)。</param>
        Task SendRawAsync(MessageEnvelope message, CancellationToken cancellationToken = default);
    }
}
