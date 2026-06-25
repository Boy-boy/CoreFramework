using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Models;

namespace Core.Scheduling.Internal
{
    /// <summary>
    /// 过滤器执行管线。把多个 <see cref="IHandlerExecutionFilter"/> 按 Order 升序套到
    /// <see cref="IScheduledHandler.ExecuteAsync"/> 外面。
    /// </summary>
    internal sealed class HandlerExecutionPipeline : IHandlerExecutionPipeline
    {
        private readonly IHandlerExecutionFilter[] _filters;

        public HandlerExecutionPipeline(IEnumerable<IHandlerExecutionFilter> filters)
        {
            _filters = filters?.OrderBy(f => f.Order).ToArray() ?? Array.Empty<IHandlerExecutionFilter>();
        }

        /// <summary>
        /// 调用管线。注意：除运行时已知的几种终止情况（取消、未被任何 filter 处理的异常）外，
        /// 异常会冒到调用方，由 <see cref="Hosting.SchedulerHostedService"/> 转成 Faulted 结果。
        /// </summary>
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
