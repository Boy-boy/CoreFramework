using Core.Ddd.Domain.Entities;
using Core.EventBus;
using Core.EventBus.Transaction;
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
            var serviceProvider = options.FindExtension<CoreOptionsExtension>().ApplicationServiceProvider;
            MessagePublisher = serviceProvider.GetService<IMessagePublisher>();
        }
        private IMessagePublisher MessagePublisher { get; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var events = GetDomainEvents();
            var result = events.Count;
            if (events.Count > 0 && MessagePublisher != null)
            {
                using (var transaction = (TransactionBase)Database.BeginTransaction(MessagePublisher))
                {
                    if (transaction == null)
                    {
                        result = base.SaveChanges(acceptAllChangesOnSuccess);
                        foreach (var item in events)
                        {
                            MessagePublisher.PublishAsync(item).GetAwaiter().GetResult();
                        }
                    }
                    else
                    {
                        foreach (var item in events)
                        {
                            MessagePublisher.PublishAsync(item).GetAwaiter().GetResult();
                        }
                        result += base.SaveChanges(acceptAllChangesOnSuccess);
                        transaction.Commit();
                    }
                }
                return result;
            }
            result = base.SaveChanges(acceptAllChangesOnSuccess);
            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var events = GetDomainEvents();
            var result = events.Count;
            if (events.Count > 0 && MessagePublisher != null)
            {
                using (var transaction = (TransactionBase)Database.BeginTransaction(MessagePublisher))
                {
                    if (transaction == null)
                    {
                        result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                        foreach (var item in events)
                        {
                            await MessagePublisher.PublishAsync(item);
                        }
                    }
                    else
                    {
                        foreach (var item in events)
                        {
                            await MessagePublisher.PublishAsync(item);
                        }
                        result += await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                return result;
            }
            result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            return result;
        }

        protected virtual List<IMessage> GetDomainEvents()
        {
            var events = new List<IMessage>();
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
    }
}
