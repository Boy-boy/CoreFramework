using System;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Internal;
using Core.Scheduling.Models;
using Microsoft.Extensions.Options;

namespace Core.Scheduling.Hosting
{
    /// <summary>
    /// BG 调度宿主使用的实现:走 <see cref="NextRunCalculator"/> 算出真正的下次时间,
    /// 失败时按指数退避(上限取 <see cref="ScheduleDescriptor.MaxBackoff"/> 或
    /// <see cref="SchedulingOptions.DefaultMaxBackoff"/>)。
    /// </summary>
    internal sealed class BackgroundNextRunStrategy : INextRunStrategy
    {
        private readonly SchedulingOptions _options;

        public BackgroundNextRunStrategy(IOptions<SchedulingOptions> options)
        {
            _options = options.Value;
        }

        public DateTimeOffset? ComputeNextRun(
            ScheduleDescriptor schedule,
            HandlerExecutionStatus status,
            int consecutiveFailures,
            DateTimeOffset finishTime)
        {
            var maxBackoff = schedule.MaxBackoff ?? _options.DefaultMaxBackoff;
            return NextRunCalculator.Compute(schedule, status, consecutiveFailures, finishTime, maxBackoff);
        }
    }
}
