using Core.Uow;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.EntityFrameworkCore.EntityFrameworkCore
{
    public class EfCoreTransactionApi : ITransactionApi
    {
        private readonly IDbContextTransaction _dbContextTransaction;
        private bool _disposed;

        public EfCoreTransactionApi(IDbContextTransaction dbContextTransaction)
        {
            _dbContextTransaction = dbContextTransaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return _dbContextTransaction.CommitAsync(cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return _dbContextTransaction.RollbackAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _dbContextTransaction.DisposeAsync();
        }
    }
}
