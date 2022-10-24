using Core.EventBus.Transaction;
using System.Threading.Tasks;
using Core.EventBus.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.EventBus
{
    public abstract class MessagePublisherBase : IMessagePublisher
    {
        public IServiceProvider ServiceProvider { get; }

        public ITransactionAccessor TransactionAccessor { get; }

        public MessagePublisherMailBox MessagePublisherMailBox { get; }

        protected IStorage Storage { get; }

        protected MessagePublisherBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            TransactionAccessor = serviceProvider.GetRequiredService<ITransactionAccessor>();
            Storage = serviceProvider.GetService<IStorage>();
            MessagePublisherMailBox = ActivatorUtilities.CreateInstance<MessagePublisherMailBox>(serviceProvider, this);
        }

        public async Task PublishAsync<T>(T message)
            where T : class, IMessage
        {
            var transaction = (TransactionBase)TransactionAccessor.Transaction;
            Storage?.StoreMessage(new MediumMessage(message), transaction?.DbTransaction);

            if (transaction == null)
            {
                MessagePublisherMailBox.EnqueueMessage(message);
            }
            else
            {
                transaction.AddMessage(message);
            }
            await Task.CompletedTask;
        }

        public abstract Task SendAsync<T>(T message)
            where T : class, IMessage;
    }
}
