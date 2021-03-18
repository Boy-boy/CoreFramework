using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Core.EventBus.Messaging;

namespace Core.EventBus.Transaction
{
    public abstract class TransactionBase : ITransaction, IDisposable
    {
        private readonly IMessagePublisher _publisher;

        private readonly ConcurrentQueue<IMessage> _messages;

        public object DbTransaction { get; set; }

        public bool AutoCommit { get; set; }

        protected TransactionBase(IMessagePublisher publisher)
        {
            _publisher = publisher;
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
            while (!_messages.IsEmpty)
            {
                _messages.TryDequeue(out var message);
                ((MessagePublisherBase)_publisher)?.SendAsync(message);
            }
        }

        public abstract void Dispose();

    }
}
