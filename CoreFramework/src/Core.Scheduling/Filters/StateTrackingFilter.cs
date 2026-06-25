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
    /// 等 handler/内层 filter 返回后,把结果写回 <see cref="HandlerStateStore"/>,
    /// NextRunTime 由 <see cref="INextRunStrategy"/> 决定:
    /// BG 模式算出退避后的具体时间;Hangfire/Quartz 模式返回 null,不汇报。
    /// </summary>
    internal sealed class StateTrackingFilter : IHandlerExecutionFilter
    {
        // handler 被并发移除(孤儿场景)时的兜底间隔。仅用于 BG 模式 cosmetic next-run 计算,
        // 因为 NextRunCalculator 不能接受 0 interval;Hangfire/Quartz strategy 返回 null,本值不会被读。
        private static readonly TimeSpan OrphanFallbackInterval = TimeSpan.FromMinutes(1);

        private readonly HandlerStateStore _store;
        private readonly IScheduledHandlerRegistry _registry;
        private readonly INextRunStrategy _nextRunStrategy;
        private readonly TimeProvider _timeProvider;

        public StateTrackingFilter(
            HandlerStateStore store,
            IScheduledHandlerRegistry registry,
            INextRunStrategy nextRunStrategy,
            TimeProvider timeProvider)
        {
            _store = store;
            _registry = registry;
            _nextRunStrategy = nextRunStrategy;
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
            var handler = _registry.Find(context.HandlerCode);
            var schedule = handler?.Schedule ?? Core.Scheduling.ScheduleDescriptor.FixedInterval(OrphanFallbackInterval);

            record.MarkFinished(finishTime, result, schedule, _nextRunStrategy);
            return result;
        }
    }
}
