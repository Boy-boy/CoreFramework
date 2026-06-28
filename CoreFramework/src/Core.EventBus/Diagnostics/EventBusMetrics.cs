using System.Diagnostics.Metrics;

namespace Core.EventBus.Diagnostics
{
    /// <summary>
    /// EventBus 的 OpenTelemetry-friendly metrics 出口;通过 <see cref="System.Diagnostics.Metrics.Meter"/>
    /// 暴露核心指标,APM / Prometheus / OpenTelemetry collector 订阅 <see cref="MeterName"/> 即可接入。
    /// </summary>
    /// <remarks>
    /// <para><b>指标列表</b></para>
    /// <list type="bullet">
    ///   <item><description><c>eventbus.outbox.dispatched_total</c> (counter): outbox 投递成功条数 — 用于跟踪吞吐</description></item>
    ///   <item><description><c>eventbus.outbox.failed_total</c> (counter): outbox 投递失败条数(已 MarkFailed,尚未进死信) — 用于告警 broker 故障</description></item>
    ///   <item><description><c>eventbus.outbox.dispatch.duration</c> (histogram, ms): 单条投递耗时分布 — p99 用于排查 broker 慢</description></item>
    ///   <item><description><c>eventbus.inbox.duplicate_total</c> (counter): inbox 命中重复消费的次数 — 重复率体现 broker 重投频率</description></item>
    ///   <item><description><c>eventbus.handler.failures_total</c> (counter): handler 抛异常次数 — 业务侧故障告警</description></item>
    ///   <item><description><c>eventbus.dead_letter_total</c> (counter): 转入死信表条数 — 永久失败累积告警</description></item>
    /// </list>
    /// <para><b>tag 维度</b>: 大多数指标带 <c>messageName</c> 和 <c>handlerType</c> tag,
    /// 便于在 Grafana / Prometheus 按消息类型 / handler 切片分析。</para>
    /// <para><b>零开销保证</b>: System.Diagnostics.Metrics 在无订阅者时近乎零成本,可安全部署到所有环境。</para>
    /// </remarks>
    public static class EventBusMetrics
    {
        /// <summary>Meter 名称;OpenTelemetry / Prometheus 导出端订阅此名即可接入。</summary>
        public const string MeterName = "Core.EventBus";

        /// <summary>Meter 版本号,跟随框架大版本走。</summary>
        public const string MeterVersion = "10.0.0";

        private static readonly Meter Meter = new(MeterName, MeterVersion);

        /// <summary>Outbox 投递成功条数。tag: messageName。</summary>
        public static readonly Counter<long> OutboxDispatched =
            Meter.CreateCounter<long>("eventbus.outbox.dispatched_total", unit: "{message}",
                description: "Outbox messages successfully delivered to broker.");

        /// <summary>Outbox 投递失败条数(已 MarkFailed,尚未进死信)。tag: messageName。</summary>
        public static readonly Counter<long> OutboxFailed =
            Meter.CreateCounter<long>("eventbus.outbox.failed_total", unit: "{message}",
                description: "Outbox messages that failed to deliver (will be retried).");

        /// <summary>单条 outbox 投递耗时分布(ms)。tag: messageName。</summary>
        public static readonly Histogram<double> OutboxDispatchDuration =
            Meter.CreateHistogram<double>("eventbus.outbox.dispatch.duration", unit: "ms",
                description: "Wall-clock duration of a single outbox message dispatch.");

        /// <summary>Inbox 命中重复消费的次数。tag: messageName, handlerType。</summary>
        public static readonly Counter<long> InboxDuplicate =
            Meter.CreateCounter<long>("eventbus.inbox.duplicate_total", unit: "{message}",
                description: "Inbox detected duplicate delivery and skipped the handler.");

        /// <summary>Handler 抛异常次数。tag: messageName, handlerType。</summary>
        public static readonly Counter<long> HandlerFailures =
            Meter.CreateCounter<long>("eventbus.handler.failures_total", unit: "{failure}",
                description: "Handler threw an exception during processing.");

        /// <summary>转入死信表条数。tag: messageName, reason (permanent/maxRetries)。</summary>
        public static readonly Counter<long> DeadLetter =
            Meter.CreateCounter<long>("eventbus.dead_letter_total", unit: "{message}",
                description: "Messages moved to the dead letter table.");
    }
}
