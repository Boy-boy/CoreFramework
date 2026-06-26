using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Models;

namespace Core.Scheduling.Abstractions
{
    /// <summary>执行管线契约:把 filter 链与最终 handler 串起来,供各宿主统一触发。</summary>
    public interface IHandlerExecutionPipeline
    {
        /// <summary>触发一次 handler 执行,经过全部已注册过滤器。</summary>
        Task<HandlerExecutionResult> InvokeAsync(
            IScheduledHandler handler,
            HandlerExecutionContext context,
            CancellationToken cancellationToken);
    }
}
