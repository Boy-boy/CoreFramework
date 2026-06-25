using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Models;

namespace Core.Scheduling.Abstractions
{
    /// <summary>
    /// 处理器执行管线契约。
    /// 把外层 <see cref="IHandlerExecutionFilter"/> 链条与最终 <see cref="IScheduledHandler.ExecuteAsync"/> 串起来,
    /// 供调度宿主（BG / Quartz / 其它）以统一姿势触发一次执行。
    /// </summary>
    public interface IHandlerExecutionPipeline
    {
        /// <summary>
        /// 触发一次 handler 执行,经过全部已注册过滤器。
        /// </summary>
        Task<HandlerExecutionResult> InvokeAsync(
            IScheduledHandler handler,
            HandlerExecutionContext context,
            CancellationToken cancellationToken);
    }
}
