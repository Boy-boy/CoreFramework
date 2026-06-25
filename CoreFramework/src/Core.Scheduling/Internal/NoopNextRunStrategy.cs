using System;
using Core.Scheduling.Models;

namespace Core.Scheduling.Internal
{
    /// <summary>
    /// Hangfire / Quartz 模式下的默认实现:不算下次时间,交给外部引擎。
    /// <see cref="HandlerState.NextRunTime"/> 在这两种模式下因此会保持 <see langword="null"/>,
    /// 避免框架报告与引擎实际触发时间不一致的"幻觉"。
    /// </summary>
    internal sealed class NoopNextRunStrategy : INextRunStrategy
    {
        public DateTimeOffset? ComputeNextRun(
            ScheduleDescriptor schedule,
            HandlerExecutionStatus status,
            int consecutiveFailures,
            DateTimeOffset finishTime)
            => null;
    }
}
