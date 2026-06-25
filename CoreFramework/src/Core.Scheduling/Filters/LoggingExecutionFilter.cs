using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Hosting;
using Core.Scheduling.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Scheduling.Filters
{
    /// <summary>
    /// 日志过滤器：在 handler 执行前后写结构化日志。
    /// 受 <see cref="SchedulingOptions.EnableLogging"/> 控制,关时直接透传 <c>next</c>,不写任何调度日志
    /// (业务自己写的日志不受影响)。
    /// </summary>
    internal sealed class LoggingExecutionFilter : IHandlerExecutionFilter
    {
        private readonly ILogger<LoggingExecutionFilter> _logger;
        private readonly IOptionsMonitor<SchedulingOptions> _options;

        public LoggingExecutionFilter(
            ILogger<LoggingExecutionFilter> logger,
            IOptionsMonitor<SchedulingOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        public int Order => 100;

        public async Task<HandlerExecutionResult> InvokeAsync(
            HandlerExecutionContext context,
            HandlerExecutionDelegate next,
            CancellationToken cancellationToken)
        {
            if (!_options.CurrentValue.EnableLogging)
                return await next(context, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Handler {HandlerCode} starting at {FireTime:O} (scheduled {ScheduledTime:O}).",
                context.HandlerCode,
                context.FireTime,
                context.ScheduledTime);

            HandlerExecutionResult result;
            try
            {
                result = await next(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Handler {HandlerCode} threw an unhandled exception.",
                    context.HandlerCode);
                throw;
            }

            switch (result.Status)
            {
                case HandlerExecutionStatus.Success:
                    _logger.LogDebug(
                        "Handler {HandlerCode} succeeded in {DurationMs} ms.",
                        context.HandlerCode,
                        (long)result.Duration.TotalMilliseconds);
                    break;
                case HandlerExecutionStatus.Skipped:
                    _logger.LogDebug(
                        "Handler {HandlerCode} skipped: {Reason}",
                        context.HandlerCode,
                        result.ErrorMessage);
                    break;
                case HandlerExecutionStatus.Failure:
                    _logger.LogWarning(
                        "Handler {HandlerCode} reported failure: {Error}",
                        context.HandlerCode,
                        result.ErrorMessage);
                    break;
                case HandlerExecutionStatus.Cancelled:
                    _logger.LogInformation(
                        "Handler {HandlerCode} cancelled by host.",
                        context.HandlerCode);
                    break;
                case HandlerExecutionStatus.Faulted:
                    _logger.LogError(result.Exception,
                        "Handler {HandlerCode} faulted: {Error}",
                        context.HandlerCode,
                        result.ErrorMessage);
                    break;
            }

            return result;
        }
    }
}
