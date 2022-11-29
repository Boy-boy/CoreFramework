using System.Threading;
using System.Threading.Tasks;

namespace Core.Uow
{
    public interface IUnitOfWork : IDatabaseApiContainer
    {
        void Commit();

        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
