using Core.Uow;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Core.EntityFrameworkCore.UnitOfWork
{
    public class EfCoreUnitOfWork : IUnitOfWork
    {
        public List<DbContext> DbContexts { get; }

        public EfCoreUnitOfWork()
        {
            DbContexts = new List<DbContext>(); ;
        }

        public void Commit()
        {
            DbContexts.ForEach(dbContext => dbContext.SaveChanges());
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task<int>>();
            DbContexts.ForEach(dbContext => tasks.Add(dbContext.SaveChangesAsync(cancellationToken)));
            return Task.WhenAll(tasks);
        }

        public void RegisterCoreDbContext(DbContext coreDbContext)
        {
            if (!DbContexts.Exists(dbCtx => dbCtx.Equals(coreDbContext)))
            {
                DbContexts.Add(coreDbContext);
            }
        }
    }
}
