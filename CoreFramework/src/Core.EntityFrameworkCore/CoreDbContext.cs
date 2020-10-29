using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore
{
    public class CoreDbContext : DbContext
    {
        public CoreDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var events = GetDomainEvents();
            TrackEventEntities(events);
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            return result;
        }

        protected virtual List<AggregateRootEvent> GetDomainEvents()
        {
            var events = new List<AggregateRootEvent>();
            foreach (var entry in ChangeTracker.Entries().ToList())
            {
                if (!(entry.Entity is AggregateRoot domainEntity)) continue;
                var domainEvents = domainEntity.GetEvents().ToList();
                if (!domainEvents.Any()) continue;
                events.AddRange(domainEvents);
                domainEntity.CleanEvents();
            }
            return events;
        }

        private void TrackEventEntities(List<AggregateRootEvent> events)
        {
            events = events ?? new List<AggregateRootEvent>();
            foreach (var @event in events)
            {
                Add(new Event(@event));
            }
        }
    }
}
