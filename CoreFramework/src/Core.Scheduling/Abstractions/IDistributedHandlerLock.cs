using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Scheduling.Abstractions
{
    /// <summary>
    /// 分布式 handler 锁:让多节点 BG 调度器对同一 <see cref="IScheduledHandler.HandlerCode"/>
    /// 同一时刻只有一个节点触发。<br/>
    /// 默认实现是 no-op(单节点直接放行);要做集群把 <c>Core.Scheduling.Redis</c> 或自家实现
    /// 通过 DI 替换即可。
    /// </summary>
    /// <remarks>
    /// 仅 BG 宿主(<c>SchedulerHostedService</c>)消费本接口。Hangfire 用自家
    /// <c>DisableConcurrentExecutionAttribute</c> + 存储层锁、Quartz 用 <c>QRTZ_LOCKS</c>,
    /// 两者都不接触本接口,因此本契约从 Abstractions 下沉到 Core.Scheduling 跟 BG 宿主同包。
    /// </remarks>
    public interface IDistributedHandlerLock
    {
        /// <summary>
        /// 尝试获取锁。
        /// </summary>
        /// <param name="handlerCode">处理器编码。</param>
        /// <param name="leaseDuration">租约时长;过期自动释放,防止节点崩溃导致锁泄漏。
        /// 建议设置为 <b>大于最长可能执行时间</b>,长任务请在执行中调 <see cref="IDistributedHandlerLockHandle.RenewAsync"/>。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>抢到锁返回句柄;别的节点已持有则返回 <see langword="null"/>。</returns>
        Task<IDistributedHandlerLockHandle?> TryAcquireAsync(
            string handlerCode,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// 分布式锁句柄。
    /// 通过 <see cref="IAsyncDisposable.DisposeAsync"/> 显式释放;TTL 过期会自动释放兜底。
    /// </summary>
    public interface IDistributedHandlerLockHandle : IAsyncDisposable
    {
        /// <summary>锁所对应的处理器编码。</summary>
        string HandlerCode { get; }

        /// <summary>
        /// 延长租约。仅当本句柄仍是锁的持有者时成功。
        /// 长任务可在执行过程中周期性调用。
        /// </summary>
        /// <returns>延长成功返回 <see langword="true"/>;已被别人抢走或已释放返回 <see langword="false"/>。</returns>
        Task<bool> RenewAsync(TimeSpan leaseDuration, CancellationToken cancellationToken);
    }
}
