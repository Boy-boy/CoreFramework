using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Transaction
{
    public interface ITransaction
    {
        bool AutoCommit { get; set; }

        void Commit();

        Task CommitAsync(CancellationToken cancellationToken = default);

        void Rollback();

        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
