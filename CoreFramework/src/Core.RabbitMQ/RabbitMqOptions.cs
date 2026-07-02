using System;

namespace Core.RabbitMQ
{
    /// <summary>
    /// 消费端 handler 抛异常后的 ack/nack 策略。
    /// </summary>
    public enum RabbitMqFailureBehavior
    {
        /// <summary>
        /// 总是 ack(旧版默认):失败消息从 broker 视角消失,完全依赖业务层 inbox 兜底。
        /// 在没有 inbox 的部署中会造成消息静默丢失。
        /// </summary>
        AlwaysAck = 0,

        /// <summary>
        /// 失败时 nack 且不重投:消息要么进 broker 配置的 DLX,要么直接丢弃。
        /// 适合"业务上接受丢失但要可观测"的场景。
        /// </summary>
        NackNoRequeue = 1,

        /// <summary>
        /// 首次失败 nack 重投,二次失败 nack 不重投(默认)。<br/>
        /// 用 <c>BasicDeliverEventArgs.Redelivered</c> 区分:
        /// 配合上层 inbox 去重达成"业务最终一次性",同时给 poison message 一个截断口避免无限循环。
        /// </summary>
        RequeueOnce = 2,
    }

    public class RabbitMqOptions
    {
        public RabbitMqConnectionConfigure Connection { get; set; }

        /// <summary>
        /// handler 失败时的 ack/nack 策略。默认 <see cref="RabbitMqFailureBehavior.RequeueOnce"/>,
        /// 让 broker 在首次失败时重投一次,与上层 inbox 去重协同达成业务最终一致性。
        /// </summary>
        public RabbitMqFailureBehavior FailureBehavior { get; set; } = RabbitMqFailureBehavior.RequeueOnce;

        /// <summary>
        /// 死信交换机名,可选。设置后 queue 声明时会带上 <c>x-dead-letter-exchange</c> 参数,
        /// <see cref="FailureBehavior"/> 在不重投的分支(NackNoRequeue 与 RequeueOnce 的二次失败)
        /// 会把消息送到该交换机,而非直接丢弃。
        /// </summary>
        /// <remarks>
        /// 启用 DLX 后还需要在 broker 端把它绑到一条 DLQ 队列上,框架不代办这一步 ——
        /// 死信处置的语义(归档 / 人工介入 / 转发其它系统)与具体业务强相关,框架只负责"把失败消息路由出去"。
        /// </remarks>
        public string DeadLetterExchange { get; set; }

        /// <summary>
        /// 死信 routing key,可选。仅在 <see cref="DeadLetterExchange"/> 已设置时生效。
        /// 留空时复用原 routing key,适用于 direct 类型的 DLX。
        /// </summary>
        public string DeadLetterRoutingKey { get; set; }

        /// <summary>
        /// <see cref="DefaultRabbitMqPersistentConnection.TryConnect"/> 内部 Polly 指数退避的重试次数;默认 6(约 126s)。
        /// 生产环境应根据 SLA 与依赖恢复窗口调整 —— 云上跨可用区通常 3~4 已够,自建集群可以放大到 8~10。
        /// </summary>
        public int ConnectionRetryCount { get; set; } = 6;

        /// <summary>
        /// consumer 侧 <c>BasicQos.prefetchCount</c>;默认 30。慢 handler 或大 payload 场景需要显式调小防止内存打爆。
        /// </summary>
        public ushort ConsumerPrefetchCount { get; set; } = 30;

        /// <summary>
        /// consumer 内部定时探活并重建 channel 的间隔,秒;默认 30。channel 死掉后最长会拖 <see cref="ConsumerRebuildIntervalSeconds"/> 才恢复,
        /// 对高吞吐链路应该缩到 5~10。
        /// </summary>
        public int ConsumerRebuildIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// consumer 首次探活 delay,秒;避免 Initialize 完刚好 timer 也在跑造成"两次建 channel"竞态。默认 2。
        /// </summary>
        public int ConsumerInitialDelaySeconds { get; set; } = 2;

        public RabbitMqOptions()
        {
            Connection = new RabbitMqConnectionConfigure();
        }

        /// <summary>启动期校验数值合理性;非法值即时抛错,避免配置错到生产才炸。</summary>
        public void ValidateNumericLimits()
        {
            if (ConnectionRetryCount < 0)
                throw new InvalidOperationException($"{nameof(RabbitMqOptions)}.{nameof(ConnectionRetryCount)} 不能为负,当前={ConnectionRetryCount}。");
            if (ConsumerPrefetchCount == 0)
                throw new InvalidOperationException($"{nameof(RabbitMqOptions)}.{nameof(ConsumerPrefetchCount)} 必须 > 0(unlimited 语义容易导致 OOM,故禁止),当前=0。");
            if (ConsumerRebuildIntervalSeconds <= 0)
                throw new InvalidOperationException($"{nameof(RabbitMqOptions)}.{nameof(ConsumerRebuildIntervalSeconds)} 必须 > 0,当前={ConsumerRebuildIntervalSeconds}。");
            if (ConsumerInitialDelaySeconds < 0)
                throw new InvalidOperationException($"{nameof(RabbitMqOptions)}.{nameof(ConsumerInitialDelaySeconds)} 不能为负,当前={ConsumerInitialDelaySeconds}。");
        }
    }
}
