using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Scheduling.DistributedLocking
{
    /// <summary>单节点默认实现:永远抢到锁,Dispose / Renew 均为空操作;DI 替换为 Redis 等实现即可获得集群仲裁。</summary>
    internal sealed class NoopDistributedHandlerLock : IDistributedHandlerLock
    {
        public Task<IDistributedHandlerLockHandle> TryAcquireAsync(
            string handlerCode,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken)
            => Task.FromResult<IDistributedHandlerLockHandle>(new NoopHandle(handlerCode));

        private sealed class NoopHandle : IDistributedHandlerLockHandle
        {
            public NoopHandle(string handlerCode) => HandlerCode = handlerCode;

            public string HandlerCode { get; }

            public Task<bool> RenewAsync(TimeSpan leaseDuration, CancellationToken cancellationToken)
                => Task.FromResult(true);

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
