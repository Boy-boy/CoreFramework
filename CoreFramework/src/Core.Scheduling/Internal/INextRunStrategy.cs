using System;
using Core.Scheduling.Models;

namespace Core.Scheduling.Internal
{
    /// <summary>
    /// 计算 handler 本次执行结束后的下次触发时间。<br/>
    /// BG 模式由本框架负责派发,会基于 schedule + 失败计数算出退避后的具体时间;<br/>
    /// Hangfire / Quartz 模式由它们自家的引擎决定下次,框架不该插手,返回 <see langword="null"/>。
    /// </summary>
    internal interface INextRunStrategy
    {
        /// <summary>
        /// 计算下次触发时间。返回 <see langword="null"/> 表示"本框架不汇报,看引擎"。
        /// </summary>
        DateTimeOffset? ComputeNextRun(
            ScheduleDescriptor schedule,
            HandlerExecutionStatus status,
            int consecutiveFailures,
            DateTimeOffset finishTime);
    }
}
