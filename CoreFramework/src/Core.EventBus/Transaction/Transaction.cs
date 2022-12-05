using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Core.EventBus.Integration;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Transaction
{
    public class Transaction : ITransaction
    {
        private readonly IIntegrationMessagePublisher _publisher;

        private readonly ConcurrentQueue<IMessage> _messages;

        public object DbTransaction { get; set; }

        protected Transaction(IServiceProvider serviceProvider)
        {
            _publisher = serviceProvider.GetRequiredService<IIntegrationMessagePublisher>();
            _messages = new ConcurrentQueue<IMessage>();
        }

        public void AddMessage(IMessage message)
        {
            _messages.Enqueue(message);
        }

        public virtual void Commit()
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Commit();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    dbContextTransaction.Commit();
                    break;
            }
            Flush();
        }

        public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Commit();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    await dbContextTransaction.CommitAsync(cancellationToken);
                    break;
            }
            Flush();
        }

        public virtual void Rollback()
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Rollback();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    dbContextTransaction.Rollback();
                    break;
            }
        }

        public virtual async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Rollback();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    await dbContextTransaction.RollbackAsync(cancellationToken);
                    break;
            }
        }

        public virtual void Dispose()
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Dispose();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    dbContextTransaction.Dispose();
                    break;
            }
        }

        protected virtual void Flush()
        {
            Task.Run(async () =>
            {
                while (!_messages.IsEmpty)
                {
                    _messages.TryDequeue(out var message);
                    await ((IntegrationMessagePublisherBase)_publisher).SendAsync(message);
                }
            });
        }
    }
}
