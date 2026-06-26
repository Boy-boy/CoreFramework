using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Scheduling.DistributedLocking
{
    /// <summary>
    /// 分布式 handler 锁:让多节点 BG 调度器对同一 <see cref="IScheduledHandler.HandlerCode"/>
    /// 同一时刻只有一个节点触发。默认 noop(单节点直接放行);要做集群把 Redis 实现等通过 DI 替换即可。
    /// </summary>
    /// <remarks>
    /// 仅 BG 宿主消费。Hangfire / Quartz 都用自家锁机制,不接触本接口,故本契约住在 Background 包。
    /// </remarks>
    public interface IDistributedHandlerLock
    {
        /// <summary>尝试获取锁。</summary>
        /// <param name="handlerCode">处理器编码。</param>
        /// <param name="leaseDuration">租约时长,过期自动释放防止节点崩溃锁泄漏;建议设置 &gt; 最长可能执行时间,长任务调 <see cref="IDistributedHandlerLockHandle.RenewAsync"/> 续租。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>抢到锁返回句柄;别的节点已持有则返回 <see langword="null"/>。</returns>
        Task<IDistributedHandlerLockHandle> TryAcquireAsync(
            string handlerCode,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken);
    }

    /// <summary>分布式锁句柄,Dispose 显式释放;TTL 过期会自动释放兜底。</summary>
    public interface IDistributedHandlerLockHandle : IAsyncDisposable
    {
        /// <summary>锁对应的处理器编码。</summary>
        string HandlerCode { get; }

        /// <summary>延长租约,仅当本句柄仍为持有者时成功(供长任务周期性调用)。</summary>
        /// <returns>成功返回 true;已被别人抢走或已释放返回 false。</returns>
        Task<bool> RenewAsync(TimeSpan leaseDuration, CancellationToken cancellationToken);
    }
}
