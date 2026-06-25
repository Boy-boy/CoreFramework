using System;
using Core.Scheduling.Models;

namespace Core.Scheduling.Abstractions
{
    /// <summary>
    /// 计算 handler 本次执行结束后的下次触发时间。<br/>
    /// 仅 BG 模式注册:由本框架负责派发,基于 schedule + 失败计数算出退避后的具体时间。<br/>
    /// Hangfire / Quartz 模式下不注册此接口——下次触发完全由各自引擎决定,框架不汇报。
    /// </summary>
    /// <remarks>
    /// 跟 <see cref="IDistributedHandlerLock"/> 同属"仅 BG 宿主消费"的契约,故同居本目录。
    /// 与共享层(<see cref="Hosting.SchedulingFilterOptions"/> / <c>IHandlerExecutionFilter</c>)
    /// 完全解耦——Hangfire/Quartz 路径下 DI 容器里不会出现 INextRunStrategy 的任何实例。
    /// </remarks>
    internal interface INextRunStrategy
    {
        /// <summary>
        /// 计算下次触发时间。返回 <see langword="null"/> 表示"按当前状态算不出下次"(例如 schedule 类型不支持),
        /// 调用方应保留 MarkStarted 写入的 tentativeNext 不动。
        /// </summary>
        DateTimeOffset? ComputeNextRun(
            ScheduleDescriptor schedule,
            HandlerExecutionStatus status,
            int consecutiveFailures,
            DateTimeOffset finishTime);
    }
}
