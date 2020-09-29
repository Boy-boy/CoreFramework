using Core.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore.UnitOfWork
{
    public class EfCoreUnitOfWork : IUnitOfWork
    {
        private readonly DbContext _dbContext;

        public EfCoreUnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext ?? throw new Exception("EfCoreUnitOfWork could not work without dbContext"); ;
        }
        public void Commit()
        {
            _dbContext.SaveChanges();
        }

        public Task CommitAsync()
        {
            return _dbContext.SaveChangesAsync();
        }
    }
}
