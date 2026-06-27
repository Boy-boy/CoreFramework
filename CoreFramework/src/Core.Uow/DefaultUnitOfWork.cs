using Core.EventBus;
using Core.EventBus.Integration;
using Core.EventBus.Local;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Core.Uow
{
    public class DefaultUnitOfWork : IUnitOfWork
    {
        private const int MaxEventDispatchIterations = 16;

        private readonly ConcurrentDictionary<string, IDatabaseApi> _databaseApis;

        private readonly ConcurrentDictionary<string, ITransactionApi> _transactionApis;

        private readonly List<IMessage> _localEvents;

        private readonly List<IMessage> _distributedEvents;

        private readonly Lock _eventLock = new();

        private readonly ILocalPublisher _localMessagePublisher;

        private readonly IIntegrationPublisher _integrationMessagePublisher;

        /// <summary>
        /// AsyncLocal 入口,Dispose 时把"当前 UoW"指针归还给上层 / 置空。
        /// 不注入它就只能依赖 <see cref="UnitOfWorkMiddleware"/> 在 finally 里清零,
        /// EventBus 的后台任务(OutboxDispatcher / InboxAwareMessageHandlerInvoker)不经过该 middleware,
        /// 会把上一轮的 disposed UoW 残留到下一轮 → 下一轮 Begin 把它当成外层 → 实际拿到的是
        /// no-op 的 ChildUnitOfWork,Commit/Rollback 都被吞掉。
        /// </summary>
        private readonly IUnitOfWorkAccessor _accessor;

        private bool _disposed;


        public UnitOfWorkOptions Options { get; private set; }

        public bool IsCompleted { get; private set; }

        public DefaultUnitOfWork(IServiceProvider serviceProvider, IUnitOfWorkAccessor accessor)
        {
            _accessor = accessor;
            _databaseApis = new ConcurrentDictionary<string, IDatabaseApi>();
            _transactionApis = new ConcurrentDictionary<string, ITransactionApi>();
            _localEvents = new List<IMessage>();
            _distributedEvents = new List<IMessage>();
            _localMessagePublisher = serviceProvider.GetService<ILocalPublisher>();
            _integrationMessagePublisher = serviceProvider.GetService<IIntegrationPublisher>();
        }

        public void Initialize(UnitOfWorkOptions options)
        {
            Options = options ?? new UnitOfWorkOptions();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (IsCompleted) return;

            await SaveChangesAsync(cancellationToken);

            var iterations = 0;
            while (HasPendingEvents())
            {
                if (++iterations > MaxEventDispatchIterations)
                {
                    throw new InvalidOperationException(
                        $"UnitOfWork 事件分发超过最大迭代次数 ({MaxEventDispatchIterations})，疑似事件循环依赖。");
                }

                var (localBatch, distributedBatch) = DrainEvents();

                foreach (var localEvent in localBatch)
                {
                    await _localMessagePublisher.PublishAsync(localEvent, cancellationToken);
                }

                foreach (var distributedEvent in distributedBatch)
                {
                    await _integrationMessagePublisher.PublishAsync(distributedEvent, cancellationToken);
                }

                await SaveChangesAsync(cancellationToken);
            }

            await CommitTransactionsAsync(cancellationToken);
            IsCompleted = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (IsCompleted) return;

            ClearPendingEvents();
            await RollbackTransactionsAsync(cancellationToken);
            IsCompleted = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var transaction in _transactionApis.Values)
            {
                try
                {
                    await transaction.DisposeAsync();
                }
                catch
                {
                    // 释放阶段吞掉异常，确保所有资源都尝试释放
                }
            }

            foreach (var databaseApi in _databaseApis.Values)
            {
                try
                {
                    await databaseApi.DisposeAsync();
                }
                catch
                {
                    // ignore
                }
            }

            _transactionApis.Clear();
            _databaseApis.Clear();
            ClearPendingEvents();

            // 把 accessor 里仍指向自己的 AsyncLocal 槽清空,避免后台任务下一轮 Begin 误当外层。
            // 注:只有"自己仍是当前 UoW"时才清,否则会破坏调用栈里别处刚 push 的新 UoW
            if (_accessor != null && ReferenceEquals(_accessor.UnitOfWork, this))
            {
                _accessor.UnitOfWork = null;
            }
        }

        private async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            foreach (var databaseApi in _databaseApis.Values)
            {
                if (databaseApi is ISupportsSavingChanges changes)
                {
                    await changes.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private async Task CommitTransactionsAsync(CancellationToken cancellationToken)
        {
            var transactions = _transactionApis.Values.ToImmutableList();
            foreach (var transaction in transactions)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }

        private async Task RollbackTransactionsAsync(CancellationToken cancellationToken)
        {
            var transactions = _transactionApis.Values.ToImmutableList();
            foreach (var transaction in transactions)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }

        private bool HasPendingEvents()
        {
            lock (_eventLock)
            {
                return _localEvents.Count > 0 || _distributedEvents.Count > 0;
            }
        }

        private (IReadOnlyList<IMessage> Local, IReadOnlyList<IMessage> Distributed) DrainEvents()
        {
            lock (_eventLock)
            {
                var local = _localEvents.ToArray();
                var distributed = _distributedEvents.ToArray();
                _localEvents.Clear();
                _distributedEvents.Clear();
                return (local, distributed);
            }
        }

        private void ClearPendingEvents()
        {
            lock (_eventLock)
            {
                _localEvents.Clear();
                _distributedEvents.Clear();
            }
        }

        #region DatabaseApiContainer
        public IDatabaseApi FindDatabaseApi([NotNull] string key)
        {
            return _databaseApis.GetValueOrDefault(key);
        }

        public void AddDatabaseApi([NotNull] string key, [NotNull] IDatabaseApi api)
        {
            if (!_databaseApis.TryAdd(key, api))
            {
                throw new InvalidOperationException("当前 UnitOfWork 中已存在相同 key 的 database API: " + key);
            }
        }

        public IDatabaseApi GetOrAddDatabaseApi([NotNull] string key, [NotNull] Func<IDatabaseApi> factory)
        {
            return _databaseApis.GetOrAdd(key, _ => factory());
        }
        #endregion

        #region TransactionApiContainer
        public virtual ITransactionApi FindTransactionApi([NotNull] string key)
        {
            return _transactionApis.GetValueOrDefault(key);
        }

        public virtual void AddTransactionApi([NotNull] string key, [NotNull] ITransactionApi api)
        {
            if (!_transactionApis.TryAdd(key, api))
            {
                throw new InvalidOperationException("当前 UnitOfWork 中已存在相同 key 的 transaction API: " + key);
            }
        }

        public virtual ITransactionApi GetOrAddTransactionApi([NotNull] string key, [NotNull] Func<ITransactionApi> factory)
        {
            return _transactionApis.GetOrAdd(key, _ => factory());
        }
        #endregion

        public void AddLocalEvent(IMessage @event)
        {
            lock (_eventLock)
            {
                _localEvents.Add(@event);
            }
        }

        public void AddDistributedEvent(IMessage @event)
        {
            lock (_eventLock)
            {
                _distributedEvents.Add(@event);
            }
        }
    }
}
