using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Internal;
using Core.Scheduling.Models;

namespace Core.Scheduling.Filters
{
    /// <summary>
    /// 状态跟踪过滤器：最贴近 handler 的内层 filter。
    /// 等 handler/内层 filter 返回后,把"最后一次执行"的结果(LastFinishTime / LastStatus /
    /// ConsecutiveFailureCount 等)写回 <see cref="HandlerStateStore"/>。
    /// </summary>
    /// <remarks>
    /// 三宿主共用。本 filter <b>不</b>算 NextRunTime——那是 BG 专属职责,
    /// 由 BG-only 的 <see cref="BackgroundNextRunFilter"/> 在本 filter 之后写入。
    /// Hangfire/Quartz 模式不注册 BackgroundNextRunFilter,NextRunTime 保留 MarkStarted 写入的
    /// tentative 值;那两种宿主下"下次触发"应以引擎自身的计算为准,本框架的快照仅供 inspector 参考。
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
