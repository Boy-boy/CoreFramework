using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Transaction
{
    public abstract class TransactionBase : ITransaction
    {
        private readonly IMessageMailBox _messageMailBox;

        private readonly ConcurrentQueue<IMessage> _messages;

        public object DbTransaction { get; set; }

        public bool AutoCommit { get; set; }

        protected TransactionBase(IServiceProvider serviceProvider)
        {
            _messageMailBox = serviceProvider.GetRequiredService<IMessageMailBox>();
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
            Task.Run(() =>
            {
                while (!_messages.IsEmpty)
                {
                    _messages.TryDequeue(out var message);
                    _messageMailBox.EnqueueMessage(message);
                }
            });
        }

        public abstract void Dispose();

    }
}
