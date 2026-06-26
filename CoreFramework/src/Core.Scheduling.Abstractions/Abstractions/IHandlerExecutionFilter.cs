using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Models;

namespace Core.Scheduling.Abstractions
{
    /// <summary>管线下一节委托,调用即把控制权交给下一个 filter 或最终 handler。</summary>
    public delegate Task<HandlerExecutionResult> HandlerExecutionDelegate(
        HandlerExecutionContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// 执行过滤器:仿 ASP.NET Core middleware,包裹一次 handler 执行,
    /// 适合做日志 / Metrics / Tracing / 重试 / 熔断 / 状态跟踪等横切关注点。
    /// </summary>
    /// <remarks>
    /// <see cref="Order"/> 越小越靠外;相同 Order 保留 DI 注册顺序。推荐范围:
    /// <list type="bullet">
    /// <item>10–99: Tracing(最外层)</item>
    /// <item>100–199: 日志</item>
    /// <item>200–299: Metrics</item>
    /// <item>300+: 业务自定义(重试 / 限流 / 熔断)</item>
    /// <item>1000+: 状态记录(最贴近 handler)</item>
    /// </list>
    /// </remarks>
    public interface IHandlerExecutionFilter
    {
        /// <summary>排序值,越小越靠外。</summary>
        int Order { get; }

        /// <summary>过滤器主体,必须 await <paramref name="next"/> 否则后续 filter 与 handler 不执行。</summary>
        Task<HandlerExecutionResult> InvokeAsync(
            HandlerExecutionContext context,
            HandlerExecutionDelegate next,
            CancellationToken cancellationToken);
    }
}
