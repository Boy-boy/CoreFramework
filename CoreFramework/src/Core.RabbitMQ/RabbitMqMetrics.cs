using System.Diagnostics.Metrics;

namespace Core.RabbitMQ
{
    /// <summary>
    /// RabbitMQ 层可采集指标。用 <see cref="Meter"/> 暴露,由 OpenTelemetry / Prometheus /
    /// dotnet-counters 等消费方按 Meter 名 <c>Core.RabbitMQ</c> 订阅。
    /// </summary>
    /// <remarks>
    /// <para><b>指标清单</b></para>
    /// <list type="bullet">
    ///   <item><description><c>rabbitmq.publish.total</c> Counter&lt;long&gt; —— publish 尝试次数,tags: exchange / routing_key / result(success|returned|confirm_timeout|nack|other)</description></item>
    ///   <item><description><c>rabbitmq.publish.duration</c> Histogram&lt;double&gt; —— publish 全程耗时(ms),tags 同上</description></item>
    ///   <item><description><c>rabbitmq.consumer.handled.total</c> Counter&lt;long&gt; —— consumer 处理消息计数,tags: queue / routing_key / success(true|false)</description></item>
    ///   <item><description><c>rabbitmq.consumer.handled.duration</c> Histogram&lt;double&gt; —— consumer handler 耗时(ms)</description></item>
    ///   <item><description><c>rabbitmq.consumer.channel_rebuild.total</c> Counter&lt;long&gt; —— consumer channel 重建次数,tags: queue / reason(initial|rebuild)</description></item>
    /// </list>
    /// <para>
    /// 用 Singleton 注入,DI 缺席时(如单测)也可以 <c>new()</c> 直接使用。
    /// 采集侧样例:<c>services.AddOpenTelemetry().WithMetrics(b =&gt; b.AddMeter("Core.RabbitMQ").AddPrometheusExporter());</c>
    /// </para>
    /// </remarks>
    public sealed class RabbitMqMetrics
    {
        public const string MeterName = "Core.RabbitMQ";

        private readonly Meter _meter;
        private readonly Counter<long> _publishTotal;
        private readonly Histogram<double> _publishDuration;
        private readonly Counter<long> _consumerHandledTotal;
        private readonly Histogram<double> _consumerHandledDuration;
        private readonly Counter<long> _consumerChannelRebuildTotal;

        public RabbitMqMetrics()
        {
            _meter = new Meter(MeterName);
            _publishTotal = _meter.CreateCounter<long>("rabbitmq.publish.total", unit: "{message}");
            _publishDuration = _meter.CreateHistogram<double>("rabbitmq.publish.duration", unit: "ms");
            _consumerHandledTotal = _meter.CreateCounter<long>("rabbitmq.consumer.handled.total", unit: "{message}");
            _consumerHandledDuration = _meter.CreateHistogram<double>("rabbitmq.consumer.handled.duration", unit: "ms");
            _consumerChannelRebuildTotal = _meter.CreateCounter<long>("rabbitmq.consumer.channel_rebuild.total", unit: "{event}");
        }

        /// <summary>由 publisher 在 publish 完成(成功或失败)后记录一次。</summary>
        /// <param name="exchange">目标 exchange 名。</param>
        /// <param name="routingKey">routing key。</param>
        /// <param name="success">是否成功。</param>
        /// <param name="errorKind">失败原因:returned / confirm_timeout / nack / other;成功时为 null。</param>
        /// <param name="elapsedMs">耗时(ms)。</param>
        public void RecordPublish(string exchange, string routingKey, bool success, string errorKind, double elapsedMs)
        {
            var result = success ? "success" : (errorKind ?? "other");
            var tags = new[]
            {
                new System.Collections.Generic.KeyValuePair<string, object>("exchange", exchange ?? string.Empty),
                new System.Collections.Generic.KeyValuePair<string, object>("routing_key", routingKey ?? string.Empty),
                new System.Collections.Generic.KeyValuePair<string, object>("result", result),
            };
            _publishTotal.Add(1, tags);
            _publishDuration.Record(elapsedMs, tags);
        }

        /// <summary>consumer 处理完一条消息后记录一次(含 ack/nack 决策)。</summary>
        public void RecordConsumerHandled(string queue, string routingKey, bool success, double elapsedMs)
        {
            var tags = new[]
            {
                new System.Collections.Generic.KeyValuePair<string, object>("queue", queue ?? string.Empty),
                new System.Collections.Generic.KeyValuePair<string, object>("routing_key", routingKey ?? string.Empty),
                new System.Collections.Generic.KeyValuePair<string, object>("success", success ? "true" : "false"),
            };
            _consumerHandledTotal.Add(1, tags);
            _consumerHandledDuration.Record(elapsedMs, tags);
        }

        /// <summary>consumer 内部 channel 首次创建或重建时记录。</summary>
        public void RecordConsumerChannelRebuild(string queue, bool isRebuild)
        {
            var tags = new[]
            {
                new System.Collections.Generic.KeyValuePair<string, object>("queue", queue ?? string.Empty),
                new System.Collections.Generic.KeyValuePair<string, object>("reason", isRebuild ? "rebuild" : "initial"),
            };
            _consumerChannelRebuildTotal.Add(1, tags);
        }
    }
}
