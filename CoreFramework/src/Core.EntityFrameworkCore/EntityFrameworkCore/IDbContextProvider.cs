using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore
{
    public interface IDbContextProvider<TDbContext>
    where TDbContext : DbContext
    {
        Task<TDbContext> GetDbContextAsync();
    }
}
