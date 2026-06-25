using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Diagnostics;
using Core.Scheduling.Hosting;
using Core.Scheduling.Models;
using Microsoft.Extensions.Options;

namespace Core.Scheduling.Filters
{
    /// <summary>
    /// 分布式追踪过滤器：为每次 handler 执行创建一个 <see cref="Activity"/>。
    /// 订阅 <see cref="SchedulingDiagnostics.ActivitySourceName"/> 即可让 OpenTelemetry 拿到 span。
    /// 受 <see cref="SchedulingFilterOptions.EnableTracing"/> 控制,关时直接透传不创建 Activity。
    /// </summary>
    internal sealed class TracingExecutionFilter : IHandlerExecutionFilter, IDisposable
    {
        private readonly ActivitySource _activitySource = new(SchedulingDiagnostics.ActivitySourceName);
        private readonly IOptionsMonitor<SchedulingFilterOptions> _options;

        public TracingExecutionFilter(IOptionsMonitor<SchedulingFilterOptions> options)
        {
            _options = options;
        }

        public int Order => 10;

        public async Task<HandlerExecutionResult> InvokeAsync(
            HandlerExecutionContext context,
            HandlerExecutionDelegate next,
            CancellationToken cancellationToken)
        {
            if (!_options.CurrentValue.EnableTracing)
                return await next(context, cancellationToken).ConfigureAwait(false);

            using var activity = _activitySource.StartActivity(
                name: $"scheduling.execute {context.HandlerCode}",
                kind: ActivityKind.Internal);

            activity?.SetTag(SchedulingDiagnostics.TagHandlerCode, context.HandlerCode);
            // 用 Unix ms (long) 代替 ISO 8601 字符串,省一次字符串分配;OTel/Jaeger 都识别 long
            activity?.SetTag("scheduling.scheduled_time_ms", context.ScheduledTime.ToUnixTimeMilliseconds());

            try
            {
                var result = await next(context, cancellationToken).ConfigureAwait(false);
                activity?.SetTag(SchedulingDiagnostics.TagStatus, StatusTag(result.Status));
                activity?.SetStatus(
                    result.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error,
                    result.ErrorMessage);
                return result;
            }
            catch (Exception ex)
            {
                activity?.SetTag(SchedulingDiagnostics.TagStatus, StatusTag(HandlerExecutionStatus.Faulted));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);
                throw;
            }
        }

        // 避免热路径上 Enum.ToString() 的反射开销
        private static string StatusTag(HandlerExecutionStatus status) => status switch
        {
            HandlerExecutionStatus.Success => nameof(HandlerExecutionStatus.Success),
            HandlerExecutionStatus.Failure => nameof(HandlerExecutionStatus.Failure),
            HandlerExecutionStatus.Skipped => nameof(HandlerExecutionStatus.Skipped),
            HandlerExecutionStatus.Cancelled => nameof(HandlerExecutionStatus.Cancelled),
            HandlerExecutionStatus.Faulted => nameof(HandlerExecutionStatus.Faulted),
            _ => status.ToString(),
        };

        public void Dispose() => _activitySource.Dispose();
    }
}
