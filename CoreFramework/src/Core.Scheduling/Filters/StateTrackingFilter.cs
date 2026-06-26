using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Internal;
using Core.Scheduling.Models;

namespace Core.Scheduling.Filters
{
    /// <summary>
    /// 状态跟踪过滤器:最贴近 handler 的内层 filter,负责把最后一次执行结果写回 <see cref="HandlerStateStore"/>。
    /// </summary>
    /// <remarks>
    /// 三宿主共用,但不算 NextRunTime——那是 BG 专属。
    /// BG 由 <c>BackgroundNextRunFilter</c> 在本 filter 之后写入;Hangfire/Quartz 模式以引擎自家计算为准。
    /// </remarks>
    internal sealed class StateTrackingFilter : IHandlerExecutionFilter
    {
        private readonly HandlerStateStore _store;
        private readonly TimeProvider _timeProvider;

        public StateTrackingFilter(
            HandlerStateStore store,
            TimeProvider timeProvider)
        {
            _store = store;
            _timeProvider = timeProvider;
        }

        public int Order => 1000;

        public async Task<HandlerExecutionResult> InvokeAsync(
            HandlerExecutionContext context,
            HandlerExecutionDelegate next,
            CancellationToken cancellationToken)
        {
            HandlerExecutionResult result;
            try
            {
                result = await next(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                var now = _timeProvider.GetUtcNow();
                result = HandlerExecutionResult.Cancelled(context.HandlerCode, context.FireTime, now);
            }
            catch (Exception ex)
            {
                var now = _timeProvider.GetUtcNow();
                result = HandlerExecutionResult.Faulted(context.HandlerCode, context.FireTime, now, ex);
            }

            var finishTime = _timeProvider.GetUtcNow();
            var record = _store.Get(context.HandlerCode);
            record.MarkFinished(finishTime, result);
            return result;
        }
    }
}
