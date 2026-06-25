using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Internal;
using Core.Scheduling.Models;

namespace Core.Scheduling.Hosting
{
    /// <summary>
    /// BG 专属:基于 <see cref="StateTrackingFilter"/> 已落定的状态,通过 <see cref="INextRunStrategy"/>
    /// 算下次触发时间并写回 store。仅 <c>AddSchedulingBackground</c> 注册;
    /// Hangfire/Quartz 模式不参与下次时间汇报,故不引入本 filter。
    /// </summary>
    /// <remarks>
    /// Order 比 <see cref="StateTrackingFilter"/>(1000) 小,管线上更靠"外层"。
    /// 出栈顺序:handler → StateTrackingFilter 写最终状态 → 本 filter 读状态算 next-run 写回。
    /// 写回检查 <c>_isRunning</c>,若新一轮已抢先 MarkStarted,本次计算结果作废,
    /// 不覆盖新执行的 tentativeNext。
    /// </remarks>
    internal sealed class BackgroundNextRunFilter : IHandlerExecutionFilter
    {
        // handler 被并发移除(孤儿场景)时的兜底 schedule。仅用于 next-run cosmetic 计算,
        // 因为 NextRunCalculator 不接受 0 interval。
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

            _store.Get(context.HandlerCode).TryUpdateNextRunTime(schedule, _strategy);
            return result;
        }
    }
}
