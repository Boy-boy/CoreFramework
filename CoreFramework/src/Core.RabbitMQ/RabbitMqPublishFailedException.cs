using System;

namespace Core.RabbitMQ
{
    /// <summary>
    /// publish 流程中可观测的失败信号的基类:发布者用它统一把"消息没真正送达"上抛给调用方。
    /// </summary>
    /// <remarks>
    /// 派生类型:
    /// <list type="bullet">
    ///   <item><description><see cref="RabbitMqPublishReturnedException"/> —— mandatory:true 时 broker 退回(无路由)</description></item>
    ///   <item><description><see cref="RabbitMqPublishUnconfirmedException"/> —— confirm 超时 / broker nack</description></item>
    /// </list>
    /// outbox dispatcher / 业务 publisher 上层据此决定是否标记失败并重试。
    /// </remarks>
    public class RabbitMqPublishFailedException : Exception
    {
        public RabbitMqPublishFailedException(string message) : base(message) { }
        public RabbitMqPublishFailedException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// 消息因为无可达路由被 broker 退回(mandatory:true + 无 queue 绑定到 routingKey)。
    /// </summary>
    public sealed class RabbitMqPublishReturnedException : RabbitMqPublishFailedException
    {
        public string Exchange { get; }
        public string RoutingKey { get; }
        public int ReplyCode { get; }
        public string ReplyText { get; }
        public string MessageId { get; }

        public RabbitMqPublishReturnedException(string exchange, string routingKey, int replyCode, string replyText, string messageId)
            : base($"RabbitMQ publish 被 broker 退回(无可用路由): exchange={exchange} routingKey={routingKey} replyCode={replyCode} replyText={replyText} messageId={messageId}")
        {
            Exchange = exchange;
            RoutingKey = routingKey;
            ReplyCode = replyCode;
            ReplyText = replyText;
            MessageId = messageId;
        }
    }

    /// <summary>
    /// publisher confirms 未在期限内得到 ack(broker nack 或确认超时)。
    /// </summary>
    public sealed class RabbitMqPublishUnconfirmedException : RabbitMqPublishFailedException
    {
        public bool TimedOut { get; }

        public RabbitMqPublishUnconfirmedException(bool timedOut)
            : base(timedOut
                ? "RabbitMQ publisher confirms 等待超时,无法判定消息是否落 broker"
                : "RabbitMQ publisher confirms 收到 nack,消息未被 broker 持久化")
        {
            TimedOut = timedOut;
        }
    }
}
