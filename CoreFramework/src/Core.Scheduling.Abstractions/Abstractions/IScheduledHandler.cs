using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Models;

namespace Core.Scheduling.Abstractions
{
    /// <summary>
    /// 调度处理器：业务侧实现该接口即接入调度运行时。
    /// 同一份实现在 BG 与 Quartz 适配器下行为一致。
    /// </summary>
    /// <remarks>
    /// 实现约定：
    /// <list type="bullet">
    /// <item><see cref="ExecuteAsync"/> 抛异常 = handler 本身崩了，运行时会记录失败并按退避策略安排下次；</item>
    /// <item>返回 <see cref="HandlerExecutionStatus.Failure"/> = 业务自己识别"这一次没成"，语义比抛异常软，不计入框架级失败堆栈；</item>
    /// <item>返回 <see cref="HandlerExecutionStatus.Skipped"/> = 本轮主动跳过（如非交易时段、前置条件未满足），不触发退避。</item>
    /// </list>
    /// </remarks>
    public interface IScheduledHandler
    {
        /// <summary>
        /// 全局唯一标识，用于状态检视、Quartz JobKey、日志/Metric 标签。
        /// 注册期会校验重复并抛出，避免静默覆盖。
        /// </summary>
        string HandlerCode { get; }

        /// <summary>
        /// 人类可读名称，用于运维面板与日志可读性，不参与逻辑判断。
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 自身的触发节奏。由 handler 自行声明，调度运行时按需翻译到对应适配器。
        /// </summary>
        ScheduleDescriptor Schedule { get; }

        /// <summary>
        /// 执行一次处理。
        /// </summary>
        /// <param name="context">本次触发的上下文。</param>
        /// <param name="cancellationToken">取消令牌；触发宿主停机或外部超时时被触发。</param>
        Task<HandlerExecutionResult> ExecuteAsync(HandlerExecutionContext context, CancellationToken cancellationToken);
    }
}
