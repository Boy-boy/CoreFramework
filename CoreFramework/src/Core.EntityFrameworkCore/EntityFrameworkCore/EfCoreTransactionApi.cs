using Core.Uow;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.EntityFrameworkCore.EntityFrameworkCore
{
    public class EfCoreTransactionApi : ITransactionApi
    {
        private readonly IDbContextTransaction _dbContextTransaction;

        public EfCoreTransactionApi(IDbContextTransaction dbContextTransaction)
        {
            _dbContextTransaction = dbContextTransaction;
        }

        public async Task CommitAsync()
        {
            await _dbContextTransaction.CommitAsync();
        }
    }
}
