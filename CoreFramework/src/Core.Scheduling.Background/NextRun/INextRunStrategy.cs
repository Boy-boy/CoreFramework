using System;
using Core.Scheduling.Models;

namespace Core.Scheduling.NextRun
{
    /// <summary>
    /// 计算 handler 本次执行结束后的下次触发时间。仅 BG 模式注册;
    /// Hangfire / Quartz 模式下次触发由各自引擎决定,本接口不参与。
    /// </summary>
    internal interface INextRunStrategy
    {
        /// <summary>
        /// 计算下次触发时间。
        /// 返回 <see langword="null"/> 表示按当前状态算不出下次(如 schedule 类型不支持),
        /// 调用方保留 MarkStarted 写入的 tentativeNext。
        /// </summary>
        DateTimeOffset? ComputeNextRun(
            ScheduleDescriptor schedule,
            HandlerExecutionStatus status,
            int consecutiveFailures,
            DateTimeOffset finishTime);
    }
}
