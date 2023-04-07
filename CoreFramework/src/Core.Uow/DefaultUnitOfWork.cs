using Core.EventBus;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Uow
{
    public class DefaultUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<string, IDatabaseApi> _databaseApis;

        private readonly Dictionary<string, ITransactionApi> _transactionApis;

        private readonly List<IMessage> _localEvents;

        private readonly List<IMessage> _distributedEvents;

        public UnitOfWorkOptions Options { get; private set; }

        public DefaultUnitOfWork(IServiceProvider serviceProvider)
        {
            _databaseApis = new Dictionary<string, IDatabaseApi>();
            _transactionApis = new Dictionary<string, ITransactionApi>();
            _localEvents = new List<IMessage>();
            _distributedEvents = new List<IMessage>();
        }

        public void Initialize(UnitOfWorkOptions options)
        {
            Options = options;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await SaveChangesAsync(cancellationToken);

            while (_localEvents.Any() || _distributedEvents.Any())
            {
                if (_localEvents.Any())
                {
                    var localEventsToBePublished = _localEvents.ToArray();
                    _localEvents.Clear();
                    //await UnitOfWorkEventPublisher.PublishLocalEventsAsync(
                    //    localEventsToBePublished
                    //);
                }

                if (_distributedEvents.Any())
                {
                    var distributedEventsToBePublished = _distributedEvents.ToArray();
                    _distributedEvents.Clear();
                    //await UnitOfWorkEventPublisher.PublishDistributedEventsAsync(
                    //    distributedEventsToBePublished
                    //);
                }

                await SaveChangesAsync(cancellationToken);
            }

            await CommitTransactionsAsync();
        }

        private async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var databaseApi in _databaseApis.Values)
            {
                if (databaseApi is ISupportsSavingChanges changes)
                {
                    await changes.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private async Task CommitTransactionsAsync()
        {
            var transactions = _transactionApis.Values.ToImmutableList();
            foreach (var transaction in transactions)
            {
                await transaction.CommitAsync();
            }
        }

        #region DatabaseApiContainer
        public IDatabaseApi FindDatabaseApi([NotNull] string key)
        {
            return _databaseApis.TryGetValue(key, out var obj)
                ? obj
                : default;
        }

        public void AddDatabaseApi([NotNull] string key, [NotNull] IDatabaseApi api)
        {
            if (_databaseApis.ContainsKey(key))
            {
                throw new Exception("There is already a database API in this unit of work with given key: " + key);
            }
            _databaseApis.Add(key, api);
        }

        public IDatabaseApi GetOrAddDatabaseApi([NotNull] string key, [NotNull] Func<IDatabaseApi> factory)
        {
            if (_databaseApis.TryGetValue(key, out var obj))
            {
                return obj;
            }
            return _databaseApis[key] = factory();
        }
        #endregion

        #region TransactionApiContainer
        public virtual ITransactionApi FindTransactionApi([NotNull] string key)
        {
            return _transactionApis.TryGetValue(key, out var obj)
                ? obj
                : default;
        }

        public virtual void AddTransactionApi([NotNull] string key, [NotNull] ITransactionApi api)
        {
            if (_transactionApis.ContainsKey(key))
            {
                throw new Exception("There is already a transaction API in this unit of work with given key: " + key);
            }

            _transactionApis.Add(key, api);
        }

        public virtual ITransactionApi GetOrAddTransactionApi([NotNull] string key, [NotNull] Func<ITransactionApi> factory)
        {
            if (_transactionApis.TryGetValue(key, out var obj))
            {
                return obj;
            }
            return _transactionApis[key] = factory();
        }
        #endregion

        public void AddLocalEvent(IMessage @event)
        {
            _localEvents.Add(@event);
        }

        public void AddDistributedEvent(IMessage @event)
        {
            _distributedEvents.Add(@event);
        }
    }
}
