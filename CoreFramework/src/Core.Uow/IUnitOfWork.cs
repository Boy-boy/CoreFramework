using Core.EventBus;

namespace Core.Uow
{
    public interface IUnitOfWork : IDatabaseApiContainer, ITransactionApiContainer, IAsyncDisposable
    {
        UnitOfWorkOptions Options { get; }

        bool IsCompleted { get; }

        Task CommitAsync(CancellationToken cancellationToken = default);

        Task RollbackAsync(CancellationToken cancellationToken = default);

        void AddLocalEvent(IMessage @event);

        void AddDistributedEvent(IMessage @event);
    }
}
