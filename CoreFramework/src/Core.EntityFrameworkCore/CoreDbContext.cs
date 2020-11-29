using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Events;
using Core.EventBus.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.EntityFrameworkCore.Sharding;

namespace Core.EntityFrameworkCore
{
    public class CoreDbContext : CoreShardingDbContext
    {
        public CoreDbContext(DbContextOptions options)
            : base(options)
        {
            var serviceProvider = options.FindExtension<CoreOptionsExtension>().ApplicationServiceProvider;
            var eventBus = serviceProvider.GetService<IEventBus>();
            EntityChangeEvent = new EntityChangeEventPublish(eventBus);
        }

        protected EntityChangeEventPublish EntityChangeEvent { get; set; }

        public DbSet<Event> Events { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var events = GetDomainEvents();
            TrackingEventEntities(events);
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            EntityChangeEvent.PublishAggregateRootEvents(events);
            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var events = GetDomainEvents();
            TrackingEventEntities(events);
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            EntityChangeEvent.PublishAggregateRootEvents(events);
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

        private void TrackingEventEntities(List<AggregateRootEvent> events)
        {
            events = events ?? new List<AggregateRootEvent>();
            foreach (var @event in events)
            {
                Add(new Event(@event));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            EntityChangeEvent = null;
        }

    }
}
