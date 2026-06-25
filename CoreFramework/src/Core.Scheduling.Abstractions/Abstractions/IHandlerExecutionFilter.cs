using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Models;

namespace Core.Scheduling.Abstractions
{
    /// <summary>
    /// 一次 handler 执行的"下一节"委托。
    /// 在 <see cref="IHandlerExecutionFilter.InvokeAsync"/> 中调用即把控制权交给下一个 filter 或最终的 handler。
    /// </summary>
    public delegate Task<HandlerExecutionResult> HandlerExecutionDelegate(
        HandlerExecutionContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// 处理器执行过滤器：仿 ASP.NET Core middleware，包裹一次 <see cref="IScheduledHandler.ExecuteAsync"/>。
    /// 适合做日志、Metrics、Tracing、重试、熔断、状态跟踪等横切关注点，
    /// 让 handler 业务代码保持纯净。
    /// </summary>
    /// <remarks>
    /// 排序：<see cref="Order"/> 越小越靠外。运行时按升序构建管线，相同 Order 的相对顺序保留 DI 注册次序。
    /// 推荐范围：
    /// <list type="bullet">
    /// <item>10–99：Tracing/Activity（最外层，覆盖整个生命周期）</item>
    /// <item>100–199：日志</item>
    /// <item>200–299：Metrics</item>
    /// <item>300+：业务侧自定义（重试、限流、熔断）</item>
    /// <item>1000+：状态记录（最贴近 handler）</item>
    /// </list>
    /// </remarks>
    public interface IHandlerExecutionFilter
    {
        /// <summary>过滤器执行顺序，越小越靠外。</summary>
        int Order { get; }

        /// <summary>过滤器主体逻辑。必须在合适时机 await <paramref name="next"/>，否则后续 filter 和 handler 不会执行。</summary>
        Task<HandlerExecutionResult> InvokeAsync(
            HandlerExecutionContext context,
            HandlerExecutionDelegate next,
            CancellationToken cancellationToken);
    }
}
