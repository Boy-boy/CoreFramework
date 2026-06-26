using System;
using Core.Scheduling.Models;
using Core.Scheduling.Options;

namespace Core.Scheduling.NextRun
{
    /// <summary>
    /// 下一次触发时间计算器,仅支持 <see cref="ScheduleKind.FixedInterval"/>。
    /// Cron 走 Quartz / Hangfire 适配器自带的触发器,不进本计算器。
    /// </summary>
    /// <remarks>
    /// 故意不依赖 <see cref="BackgroundSchedulingOptions"/>:maxBackoff 显式入参,
    /// 由 <see cref="BackgroundNextRunStrategy"/> 决定取值;Hangfire / Quartz 不调用本计算器。
    /// </remarks>
    internal static class NextRunCalculator
    {
        /// <summary>根据本次执行结果与连续失败数决定下一次触发时间。</summary>
        /// <param name="schedule">handler 的调度描述符。</param>
        /// <param name="status">本次结果状态。</param>
        /// <param name="consecutiveFailures">提交本次结果后的连续失败次数(含本次)。</param>
        /// <param name="now">当前时间(通常是 finishTime)。</param>
        /// <param name="maxBackoff">退避时间上限。</param>
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
                    + "Reference Core.Scheduling.Quartz (SchedulingQuartzModule) or Core.Scheduling.Hangfire (SchedulingHangfireModule) instead.");
            }

            var interval = schedule.Interval;

            if (status == HandlerExecutionStatus.Success
                || status == HandlerExecutionStatus.Skipped
                || status == HandlerExecutionStatus.Cancelled)
            {
                return now + interval;
            }

            // Failure / Faulted:指数退避,受 maxBackoff 上限
            var capped = Math.Clamp(consecutiveFailures, 1, 6);
            var factor = Math.Pow(2, capped - 1);
            var backoffMs = interval.TotalMilliseconds * factor;
            var backoff = TimeSpan.FromMilliseconds(backoffMs);
            if (backoff > maxBackoff) backoff = maxBackoff;
            return now + backoff;
        }
    }
}
