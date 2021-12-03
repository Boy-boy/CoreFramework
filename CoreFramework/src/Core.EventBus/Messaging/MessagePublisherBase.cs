using Core.EventBus.Transaction;
using System.Threading.Tasks;
using Core.EventBus.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    public abstract class MessagePublisherBase : IMessagePublisher
    {
        public IServiceScopeFactory ServiceScopeFactory { get; }

        public ITransactionAccessor TransactionAccessor { get; }

        public MessagePublisherMailBox MessagePublisherMailBox { get; }

        protected IStorage Storage { get; }

        protected MessagePublisherBase(IServiceScopeFactory serviceScopeFactory)
        {
            ServiceScopeFactory = serviceScopeFactory;
            var provider = ServiceScopeFactory.CreateScope().ServiceProvider;
            TransactionAccessor = provider.GetRequiredService<ITransactionAccessor>();
            Storage = provider.GetService<IStorage>();
            MessagePublisherMailBox = ActivatorUtilities.CreateInstance<MessagePublisherMailBox>(provider, this);
        }

        public async Task PublishAsync<T>(T message)
            where T : class, IMessage
        {
            if (Storage == null)
            {
                MessagePublisherMailBox.EnqueueMessage(message);
            }
            else
            {
                var transaction = (TransactionBase)TransactionAccessor.Transaction;
                Storage.StoreMessage(new MediumMessage(message), transaction?.DbTransaction);

                if (transaction == null)
                {
                    //未开启事务
                    MessagePublisherMailBox.EnqueueMessage(message);
                }
                else
                {
                    if (transaction.AutoCommit)
                    {
                        await TransactionAccessor.Transaction.CommitAsync();
                        MessagePublisherMailBox.EnqueueMessage(message);
                    }
                    else
                    {
                        transaction.AddMessage(message);
                    }
                }
            }
            await Task.CompletedTask;
        }

        public abstract Task SendAsync<T>(T message)
            where T : class, IMessage;
    }
}
