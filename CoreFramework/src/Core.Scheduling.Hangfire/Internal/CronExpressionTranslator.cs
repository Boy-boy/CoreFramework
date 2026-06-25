using System;
using Core.Scheduling;

namespace Core.Scheduling.Hangfire.Internal
{
    /// <summary>
    /// 把 <see cref="ScheduleDescriptor"/> 翻译成 Hangfire / Cronos 可识别的 cron 表达式。
    /// </summary>
    /// <remarks>
    /// Hangfire 1.8+ 内置 Cronos,5 字段为分钟级,6 字段为秒级。
    /// 本翻译器仅对**整分 / 整时 / 整日**的固定间隔生成等效 cron;非整除或秒级直接抛异常,
    /// 提示消费者改用 BG 或 Quartz 适配器。
    /// </remarks>
    internal static class CronExpressionTranslator
    {
        /// <summary>翻译固定间隔到 5 字段 cron。</summary>
        /// <exception cref="InvalidOperationException">当间隔 &lt; 60 秒,或不能被整分/整时/整日整除时。</exception>
        public static string TranslateFixedInterval(TimeSpan interval, string handlerCode)
        {
            if (interval <= TimeSpan.Zero)
                throw new InvalidOperationException(
                    $"Handler '{handlerCode}' has non-positive Interval.");

            if (interval.TotalSeconds < 60)
                throw new InvalidOperationException(
                    $"Handler '{handlerCode}' has sub-minute interval ({interval}). "
                    + "Hangfire RecurringJob granularity is minute-level. "
                    + "Use BG (CoreSchedulingModule) or Quartz (CoreSchedulingQuartzModule) for sub-minute schedules.");

            // 整分钟
            var totalMinutes = (long)interval.TotalMinutes;
            if (totalMinutes * 60 == (long)interval.TotalSeconds)
            {
                if (totalMinutes < 60)
                    return $"*/{totalMinutes} * * * *";

                var totalHours = (long)interval.TotalHours;
                if (totalHours * 60 == totalMinutes && totalHours < 24)
                    return $"0 */{totalHours} * * *";

                var totalDays = (long)interval.TotalDays;
                if (totalDays * 24 == totalHours && totalDays <= 31)
                    return $"0 0 */{totalDays} * *";
            }

            throw new InvalidOperationException(
                $"Handler '{handlerCode}' has interval {interval} that cannot be expressed as a clean Hangfire cron. "
                + "Use Quartz adapter for arbitrary TimeSpan intervals, or supply ScheduleDescriptor.Cron(\"...\") directly.");
        }
    }
}
