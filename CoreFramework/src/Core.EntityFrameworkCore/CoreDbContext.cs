using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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
            var serviceProvider = options.FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider;
            DomainEventBus = serviceProvider?.GetRequiredService<IDomainEventBus>();
        }
        private IDomainEventBus DomainEventBus { get; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var events = GetDomainEvents();
            if (events.Count <= 0)
                return base.SaveChanges(acceptAllChangesOnSuccess);
            foreach (var @event in events)
            {
                DomainEventBus.PublishAsync(@event).GetAwaiter().GetResult();
            }
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var events = GetDomainEvents();
            if (events.Count <= 0)
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            foreach (var @event in events)
            {
                await DomainEventBus.PublishAsync(@event);
            }
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected virtual List<IDomainEvent> GetDomainEvents()
        {
            var events = new List<IDomainEvent>();
            foreach (var entry in ChangeTracker.Entries().ToList())
            {
                if (entry.Entity is not AggregateRoot domainEntity) continue;
                var domainEvents = domainEntity.GetEvents().ToList();
                if (!domainEvents.Any()) continue;
                events.AddRange(domainEvents);
                domainEntity.CleanEvents();
            }
            return events;
        }
    }
}
