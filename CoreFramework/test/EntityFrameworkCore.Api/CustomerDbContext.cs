using Core.EntityFrameworkCore;
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
    }
}
