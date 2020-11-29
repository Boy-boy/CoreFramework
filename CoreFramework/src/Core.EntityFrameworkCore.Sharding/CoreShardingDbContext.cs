using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Core.EntityFrameworkCore.Sharding
{
    public class CoreShardingDbContext : DbContext
    {
        public CoreShardingDbContext(DbContextOptions options)
            : base(options)
        {
        }
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ((Model)Model).TryFinalizeModel();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            ((Model)Model).TryFinalizeModel();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
