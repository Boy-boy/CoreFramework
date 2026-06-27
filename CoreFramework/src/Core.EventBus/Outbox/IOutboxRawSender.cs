using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Outbox
{
    /// <summary>
    /// "绕过 outbox 的原始投递"接口，<b>仅供 dispatcher 内部使用</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 为什么需要这个接口：业务侧的发布走 <see cref="Integration.IIntegrationPublisher.PublishAsync{T}"/>，
    /// 该方法在检测到 outbox 上下文时会把消息写进 outbox 表 —— 这是我们要的行为。但当
    /// dispatcher 从 outbox 表读取消息要真正送到 broker 时，再走 PublishAsync 会形成
    /// "outbox → outbox" 的死循环。因此独立出 <see cref="SendRawAsync"/>：它接收已经
    /// 序列化好的 <see cref="MessageEnvelope"/>，直接写入 broker（如 RabbitMQ exchange）。
    /// </para>
    /// <para>
    /// 实现注意事项：
    /// <list type="bullet">
    ///   <item><description>不应对 message 内容做任何加工（schema 校验、再序列化等），
    ///   payload 必须与生产者写入时完全一致，保证消费端反序列化得到的对象一致。</description></item>
    ///   <item><description>routing key / topic 选择应优先尊重消息类型上的 <c>[MessageName]</c>
    ///   特性，回退到 <see cref="MessageEnvelope.MessageName"/> 原值。</description></item>
    ///   <item><description>broker 临时不可达时通过 Polly 等机制做小范围重试，更大范围的重试
    ///   交给 dispatcher 通过 <c>RetryCount</c> + 退避来做。</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IOutboxRawSender
    {
        /// <summary>
        /// 将一条 outbox 中的消息原样发送到 broker。
        /// </summary>
        /// <param name="message">已从 outbox 表读出的消息载体（payload 已是 JSON）。</param>
        /// <param name="cancellationToken">取消令牌；dispatcher 进程关闭时会被触发。</param>
        Task SendRawAsync(MessageEnvelope message, CancellationToken cancellationToken = default);
    }
}
