using Microsoft.EntityFrameworkCore;

namespace Core.EntityFrameworkCore
{
    public class CoreDbContext : DbContext
    {
        public CoreDbContext(DbContextOptions options)
            : base(options)
        {

        }
    }
}
