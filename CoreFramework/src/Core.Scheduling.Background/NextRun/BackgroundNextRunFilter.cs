using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Internal;
using Core.Scheduling.Models;

namespace Core.Scheduling.NextRun
{
    /// <summary>
    /// BG 专属:基于 <see cref="StateTrackingFilter"/> 已落定的状态算下次触发时间并写回 store。
    /// Hangfire / Quartz 不引入本 filter。
    /// </summary>
    /// <remarks>
    /// Order 比 <see cref="StateTrackingFilter"/>(1000) 小,管线上更靠外。
    /// 出栈顺序:handler → StateTrackingFilter 写最终状态 → 本 filter 读状态算 next-run 写回。
    /// 写回时检查 <c>_isRunning</c>,若新一轮已抢先 MarkStarted 则放弃本次结果,不覆盖新执行的 tentativeNext。
    /// </remarks>
    internal sealed class BackgroundNextRunFilter : IHandlerExecutionFilter
    {
        // handler 被并发移除(孤儿)时的兜底 schedule;仅用于 next-run 计算,NextRunCalculator 不接受 0 interval
        private static readonly TimeSpan OrphanFallbackInterval = TimeSpan.FromMinutes(1);

        private readonly HandlerStateStore _store;
        private readonly IScheduledHandlerRegistry _registry;
        private readonly INextRunStrategy _strategy;

        public BackgroundNextRunFilter(
            HandlerStateStore store,
            IScheduledHandlerRegistry registry,
            INextRunStrategy strategy)
        {
            _store = store;
            _registry = registry;
            _strategy = strategy;
        }

        public int Order => 900;

        public async Task<HandlerExecutionResult> InvokeAsync(
            HandlerExecutionContext context,
            HandlerExecutionDelegate next,
            CancellationToken cancellationToken)
        {
            var result = await next(context, cancellationToken).ConfigureAwait(false);

            var handler = _registry.Find(context.HandlerCode);
            var schedule = handler?.Schedule ?? ScheduleDescriptor.FixedInterval(OrphanFallbackInterval);

            _store.Get(context.HandlerCode).TryUpdateNextRunTime(
                (status, fails, finish) => _strategy.ComputeNextRun(schedule, status, fails, finish));
            return result;
        }
    }
}
