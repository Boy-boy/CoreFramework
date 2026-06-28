namespace Core.EventBus.RabbitMQ
{
    /// <summary>RabbitMQ 消费端遇到反序列化失败 / Id 校验失败的"毒消息"时的处置策略。</summary>
    /// <remarks>
    /// <para>反序列化/Id 失败的消息<b>同一 payload 重投永远会再次失败</b>,需要一个明确的策略而不是默认行为。</para>
    /// <list type="bullet">
    ///   <item><description><see cref="SkipAndAck"/>:LogError + return,底层 Consumer 视作 processed=true 自动 ACK 推进 offset。
    ///     <b>不会触发 DLX</b>(ACK 不算 dead-letter),可观测性只能依赖 LogError + ConsumeError 诊断。默认值。</description></item>
    ///   <item><description><see cref="ThrowAndLetBrokerHandle"/>:LogError + 抛 <see cref="System.IO.InvalidDataException"/>。
    ///     由底层 Consumer 按 <see cref="Core.RabbitMQ.RabbitMqFailureBehavior"/> 决定 nack/requeue;
    ///     配合 <c>NackNoRequeue</c> + DLX 即可让毒消息进入 dead-letter exchange 集中排查。
    ///     代价:配 <c>RequeueOnce</c> 时同一条毒消息会被 broker 重投一次后才丢,期间 queue 头部消费会被推迟。</description></item>
    /// </list>
    /// </remarks>
    public enum PoisonMessageBehavior
    {
        /// <summary>跳过并 ACK,推进 offset。安全但 DLX 收不到毒消息。</summary>
        SkipAndAck = 0,

        /// <summary>抛异常给底层 Consumer,按 FailureBehavior 决策 nack 路径,可配合 DLX 收集毒消息。</summary>
        ThrowAndLetBrokerHandle = 1,
    }
}
