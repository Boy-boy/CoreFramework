using Core.EventBus;
using System.Diagnostics.CodeAnalysis;

namespace Core.Uow
{
    /// <summary>
    /// 嵌套场景下的占位 UoW：所有读写转发到外层真实 UoW；
    /// Commit/Rollback/Dispose 全部 no-op，由外层负责。
    /// </summary>
    internal sealed class ChildUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _outer;

        public ChildUnitOfWork(IUnitOfWork outer)
        {
            _outer = outer;
        }

        public UnitOfWorkOptions Options => _outer.Options;

        public bool IsCompleted => _outer.IsCompleted;

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => default;

        public void AddLocalEvent(IMessage @event) => _outer.AddLocalEvent(@event);

        public void AddDistributedEvent(IMessage @event) => _outer.AddDistributedEvent(@event);

        public IDatabaseApi FindDatabaseApi([NotNull] string key) => _outer.FindDatabaseApi(key);

        public void AddDatabaseApi([NotNull] string key, [NotNull] IDatabaseApi api) => _outer.AddDatabaseApi(key, api);

        public IDatabaseApi GetOrAddDatabaseApi([NotNull] string key, [NotNull] Func<IDatabaseApi> factory)
            => _outer.GetOrAddDatabaseApi(key, factory);

        public ITransactionApi FindTransactionApi([NotNull] string key) => _outer.FindTransactionApi(key);

        public void AddTransactionApi([NotNull] string key, [NotNull] ITransactionApi api) => _outer.AddTransactionApi(key, api);

        public ITransactionApi GetOrAddTransactionApi([NotNull] string key, [NotNull] Func<ITransactionApi> factory)
            => _outer.GetOrAddTransactionApi(key, factory);
    }
}
