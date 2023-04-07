using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Events;
using Core.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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
            UnitOfWorkAccessor = serviceProvider?.GetRequiredService<IUnitOfWorkAccessor>();
        }
        private IUnitOfWorkAccessor UnitOfWorkAccessor { get; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var eventReport = CreateEventReport();

            var result = base.SaveChanges(acceptAllChangesOnSuccess);

            PublishEntityEvents(eventReport);
            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var eventReport = CreateEventReport();

            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            PublishEntityEvents(eventReport);
            return result;
        }

        private void PublishEntityEvents(EntityEventReport changeReport)
        {
            var unitOfWork = UnitOfWorkAccessor.UnitOfWork;
            foreach (var localEvent in changeReport.DomainEvents)
            {
                unitOfWork.AddLocalEvent(localEvent);
            }

            foreach (var distributedEvent in changeReport.DistributedEvents)
            {
                unitOfWork.AddDistributedEvent(distributedEvent);
            }
        }

        protected virtual EntityEventReport CreateEventReport()
        {
            var eventReport = new EntityEventReport();
            foreach (var entry in ChangeTracker.Entries().ToList())
            {
                if (entry.Entity is not AggregateRoot domainEntity) continue;

                var domainEvents = domainEntity.GetLocalEvents().ToList();
                if (domainEvents.Any())
                {
                    eventReport.DomainEvents.AddRange(domainEvents);
                    domainEntity.CleanLocalEvents();
                }

                var distributedEvents = domainEntity.GetDistributedEvents().ToList();
                if (distributedEvents.Any())
                {
                    eventReport.DistributedEvents.AddRange(distributedEvents);
                    domainEntity.CleanDistributedEvents();
                }
            }
            return eventReport;
        }
    }
}
