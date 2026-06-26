using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Diagnostics;
using Core.Scheduling.Options;
using Core.Scheduling.Models;
using Microsoft.Extensions.Options;

namespace Core.Scheduling.Filters
{
    /// <summary>
    /// Metrics 过滤器:把执行次数 / 耗时 / 状态发到 <see cref="SchedulingDiagnostics.MeterName"/> Meter,
    /// 消费者订阅同名 Meter 即可对接 OpenTelemetry / Prometheus。
    /// 受 <see cref="SchedulingFilterOptions.EnableMetrics"/> 控制,关时直接透传。
    /// </summary>
    internal sealed class MetricsExecutionFilter : IHandlerExecutionFilter, IDisposable
    {
        private readonly Meter _meter;
        private readonly Counter<long> _executionCounter;
        private readonly Histogram<double> _executionDuration;
        private readonly IOptionsMonitor<SchedulingFilterOptions> _options;

        public MetricsExecutionFilter(IOptionsMonitor<SchedulingFilterOptions> options)
        {
            _options = options;
            _meter = new Meter(SchedulingDiagnostics.MeterName);
            _executionCounter = _meter.CreateCounter<long>(
                SchedulingDiagnostics.MetricExecutionCount,
                unit: "executions",
                description: "Total scheduled handler executions, tagged by handler.code and status.");
            _executionDuration = _meter.CreateHistogram<double>(
                SchedulingDiagnostics.MetricExecutionDuration,
                unit: "ms",
                description: "Scheduled handler execution duration in milliseconds.");
        }

        public int Order => 200;

        public async Task<HandlerExecutionResult> InvokeAsync(
            HandlerExecutionContext context,
            HandlerExecutionDelegate next,
            CancellationToken cancellationToken)
        {
            if (!_options.CurrentValue.EnableMetrics)
                return await next(context, cancellationToken).ConfigureAwait(false);

            HandlerExecutionResult result;
            HandlerExecutionStatus statusForTag;
            try
            {
                result = await next(context, cancellationToken).ConfigureAwait(false);
                statusForTag = result.Status;
            }
            catch
            {
                statusForTag = HandlerExecutionStatus.Faulted;
                EmitMetrics(context.HandlerCode, statusForTag, 0);
                throw;
            }

            EmitMetrics(context.HandlerCode, statusForTag, result.Duration.TotalMilliseconds);
            return result;
        }

        private void EmitMetrics(string handlerCode, HandlerExecutionStatus status, double durationMs)
        {
            var codeTag = new KeyValuePair<string, object>(SchedulingDiagnostics.TagHandlerCode, handlerCode);
            var statusTag = new KeyValuePair<string, object>(SchedulingDiagnostics.TagStatus, StatusTag(status));

            _executionCounter.Add(1, codeTag, statusTag);
            if (durationMs > 0)
                _executionDuration.Record(durationMs, codeTag, statusTag);
        }

        // 避免热路径 Enum.ToString() 的反射开销
        private static string StatusTag(HandlerExecutionStatus status) => status switch
        {
            HandlerExecutionStatus.Success => nameof(HandlerExecutionStatus.Success),
            HandlerExecutionStatus.Failure => nameof(HandlerExecutionStatus.Failure),
            HandlerExecutionStatus.Skipped => nameof(HandlerExecutionStatus.Skipped),
            HandlerExecutionStatus.Cancelled => nameof(HandlerExecutionStatus.Cancelled),
            HandlerExecutionStatus.Faulted => nameof(HandlerExecutionStatus.Faulted),
            _ => status.ToString(),
        };

        public void Dispose() => _meter.Dispose();
    }
}
