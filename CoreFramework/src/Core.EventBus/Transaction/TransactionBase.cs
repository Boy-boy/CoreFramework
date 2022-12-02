using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Core.EventBus.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Transaction
{
    public abstract class TransactionBase : ITransaction
    {
        private readonly IIntegrationMessagePublisher _publisher;

        private readonly ConcurrentQueue<IMessage> _messages;

        public object DbTransaction { get; set; }

        protected TransactionBase(IServiceProvider serviceProvider)
        {
            _publisher = serviceProvider.GetRequiredService<IIntegrationMessagePublisher>();
            _messages = new ConcurrentQueue<IMessage>();
        }

        public void AddMessage(IMessage message)
        {
            _messages.Enqueue(message);
        }

        public abstract void Commit();

        public abstract Task CommitAsync(CancellationToken cancellationToken = default);

        public abstract void Rollback();

        public abstract Task RollbackAsync(CancellationToken cancellationToken = default);

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

        public abstract void Dispose();

    }
}
