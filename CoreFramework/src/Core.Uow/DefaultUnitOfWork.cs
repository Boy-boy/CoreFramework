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

        private readonly ILocalMessagePublisher _localMessagePublisher;

        private readonly IIntegrationMessagePublisher _integrationMessagePublisher;

        private bool _disposed;


        public UnitOfWorkOptions Options { get; private set; }

        public bool IsCompleted { get; private set; }

        public DefaultUnitOfWork(IServiceProvider serviceProvider)
        {
            _databaseApis = new ConcurrentDictionary<string, IDatabaseApi>();
            _transactionApis = new ConcurrentDictionary<string, ITransactionApi>();
            _localEvents = new List<IMessage>();
            _distributedEvents = new List<IMessage>();
            _localMessagePublisher = serviceProvider.GetService<ILocalMessagePublisher>();
            _integrationMessagePublisher = serviceProvider.GetService<IIntegrationMessagePublisher>();
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
