using System;
using Core.Scheduling.Models;
using Core.Scheduling.Options;
using Microsoft.Extensions.Options;

namespace Core.Scheduling.NextRun
{
    /// <summary>
    /// BG 宿主使用的下次时间策略:走 <see cref="NextRunCalculator"/> 算时间,
    /// 失败时按指数退避(上限取 <see cref="ScheduleDescriptor.MaxBackoff"/> 或 <see cref="BackgroundSchedulingOptions.DefaultMaxBackoff"/>)。
    /// </summary>
    internal sealed class BackgroundNextRunStrategy : INextRunStrategy
    {
        private readonly BackgroundSchedulingOptions _options;

        public BackgroundNextRunStrategy(IOptions<BackgroundSchedulingOptions> options)
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
