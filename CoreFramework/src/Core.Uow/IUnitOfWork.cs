using Core.EventBus;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Uow
{
    public interface IUnitOfWork : IDatabaseApiContainer, ITransactionApiContainer
    {
        UnitOfWorkOptions Options { get; }

        Task CommitAsync(CancellationToken cancellationToken = default);

        void AddLocalEvent(IMessage @event);

        void AddDistributedEvent(IMessage @event);
    }
}
