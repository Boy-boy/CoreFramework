using System.Threading.Tasks;

namespace Core.EntityFrameworkCore.UnitOfWork
{
    public  interface IUnitOfWork
    {
        void Commit();
        Task CommitAsync();
    }
}
