using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Models;

namespace Core.Scheduling.Internal
{
    /// <summary>把多个 filter 按 Order 升序套到 handler 外面,形成执行管线。</summary>
    internal sealed class HandlerExecutionPipeline : IHandlerExecutionPipeline
    {
        private readonly IHandlerExecutionFilter[] _filters;

        public HandlerExecutionPipeline(IEnumerable<IHandlerExecutionFilter> filters)
        {
            _filters = filters?.OrderBy(f => f.Order).ToArray() ?? Array.Empty<IHandlerExecutionFilter>();
        }

        /// <summary>触发管线;未被 filter 处理的异常会冒到调用方,由宿主统一转成 Faulted 结果。</summary>
        public Task<HandlerExecutionResult> InvokeAsync(
            IScheduledHandler handler,
            HandlerExecutionContext context,
            CancellationToken cancellationToken)
        {
            HandlerExecutionDelegate next = handler.ExecuteAsync;
            for (var i = _filters.Length - 1; i >= 0; i--)
            {
                var filter = _filters[i];
                var captured = next;
                next = (ctx, ct) => filter.InvokeAsync(ctx, captured, ct);
            }

            return next(context, cancellationToken);
        }
    }
}
