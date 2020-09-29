using Core.Uow;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore.UnitOfWork
{
    public class EfCoreUnitOfWork : IUnitOfWork
    {
        public List<CoreDbContext> DbContexts { get; }

        public EfCoreUnitOfWork()
        {
            DbContexts = new List<CoreDbContext>(); ;
        }
        public void Commit()
        {
            DbContexts.ForEach(dbContext => dbContext.SaveChanges());
        }

        public Task CommitAsync()
        {
            var tasks = new List<Task<int>>();
            DbContexts.ForEach(dbContext => tasks.Add(dbContext.SaveChangesAsync()));
            return Task.WhenAll(tasks);
        }

        public void RegisterCoreDbContext(CoreDbContext coreDbContext)
        {
            if (!DbContexts.Exists(dbCtx => dbCtx.Equals(coreDbContext)))
            {
                DbContexts.Add(coreDbContext);
            }
        }
    }
}
