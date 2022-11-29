using Core.Uow;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Threading;

namespace Core.EntityFrameworkCore.EntityFrameworkCore
{
    public class EfCoreDatabaseApi : IDatabaseApi, ISupportsSavingChanges
    {
        public DbContext DbContext { get; }

        public EfCoreDatabaseApi(DbContext dbContext)
        {
            DbContext = dbContext;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return DbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
