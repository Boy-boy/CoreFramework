using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;

namespace Core.Scheduling.Hosting
{
    /// <summary>
    /// 单节点默认实现:永远抢到锁,Dispose / Renew 均是空操作。
    /// 通过 DI 替换为 Redis / SqlServer 等真实实现即可获得集群仲裁。
    /// </summary>
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
