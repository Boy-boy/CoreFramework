using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Models;

namespace Core.Scheduling.Abstractions
{
    /// <summary>调度处理器：实现此接口即接入调度运行时,BG / Quartz 适配器下行为一致。</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>抛异常 → 框架级失败,按退避策略安排下次;</item>
    /// <item>返回 <see cref="HandlerExecutionStatus.Failure"/> → 业务级失败,不计入框架失败堆栈;</item>
    /// <item>返回 <see cref="HandlerExecutionStatus.Skipped"/> → 本轮跳过,不触发退避。</item>
    /// </list>
    /// </remarks>
    public interface IScheduledHandler
    {
        /// <summary>全局唯一编码,用于状态追踪、JobKey、日志/Metric 标签;重复注册会抛异常。</summary>
        string HandlerCode { get; }

        /// <summary>人类可读名称,仅用于面板与日志展示。</summary>
        string DisplayName { get; }

        /// <summary>触发节奏,由 handler 自身声明,运行时翻译到对应适配器。</summary>
        ScheduleDescriptor Schedule { get; }

        /// <summary>执行一次处理。</summary>
        /// <param name="context">本次触发的上下文。</param>
        /// <param name="cancellationToken">取消令牌(宿主停机或外部超时时触发)。</param>
        Task<HandlerExecutionResult> ExecuteAsync(HandlerExecutionContext context, CancellationToken cancellationToken);
    }
}
