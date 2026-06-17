using Core.EntityFrameworkCore;
using Core.EventBus.Storage.EfCore;
using EntityFrameworkCore.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Api
{
    public class CustomerDbContext : CoreDbContext
    {
        public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddEventBusStorage();
        }
    }
}
