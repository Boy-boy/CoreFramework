using System;
using Core.Scheduling.Models;

namespace Core.Scheduling.Internal
{
    /// <summary>
    /// 下一次触发时间计算器。仅支持 <see cref="ScheduleKind.FixedInterval"/>;
    /// Cron 需要走 Quartz 适配器(其自带触发器)。
    /// </summary>
    /// <remarks>
    /// 故意不依赖 <see cref="Hosting.SchedulingOptions"/>:把 <c>maxBackoff</c> 作为参数显式传入,
    /// 由 BG 的 <see cref="Hosting.BackgroundNextRunStrategy"/> 决定如何取这个值。
    /// Hangfire/Quartz 不调用本计算器——它们的下次触发完全交给各自引擎。
    /// </remarks>
    internal static class NextRunCalculator
    {
        /// <summary>
        /// 根据本次执行结果与连续失败数,决定下一次触发时间。
        /// </summary>
        /// <param name="schedule">handler 的调度描述符。</param>
        /// <param name="status">本次结果状态。</param>
        /// <param name="consecutiveFailures">提交本次结果后的连续失败次数(包含本次)。</param>
        /// <param name="now">当前时间(通常是 finishTime)。</param>
        /// <param name="maxBackoff">退避时间上限(由 strategy 从 schedule + options 算出)。</param>
        public static DateTimeOffset Compute(
            ScheduleDescriptor schedule,
            HandlerExecutionStatus status,
            int consecutiveFailures,
            DateTimeOffset now,
            TimeSpan maxBackoff)
        {
            if (schedule.Kind == ScheduleKind.Cron)
            {
                throw new NotSupportedException(
                    $"ScheduleKind.Cron is not supported by the default BackgroundService runtime. "
                    + "Reference Core.Scheduling.Quartz and register it via SchedulingQuartzModule.");
            }

            var interval = schedule.Interval;

            if (status == HandlerExecutionStatus.Success
                || status == HandlerExecutionStatus.Skipped
                || status == HandlerExecutionStatus.Cancelled)
            {
                return now + interval;
            }

            // Failure / Faulted: exponential backoff capped by maxBackoff.
            var capped = Math.Clamp(consecutiveFailures, 1, 6);
            var factor = Math.Pow(2, capped - 1);
            var backoffMs = interval.TotalMilliseconds * factor;
            var backoff = TimeSpan.FromMilliseconds(backoffMs);
            if (backoff > maxBackoff) backoff = maxBackoff;
            return now + backoff;
        }
    }
}
