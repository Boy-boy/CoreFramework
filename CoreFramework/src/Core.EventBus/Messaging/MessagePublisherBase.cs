using Core.EventBus.Transaction;
using System.Threading.Tasks;
using Core.EventBus.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Messaging
{
    public abstract class MessagePublisherBase : IMessagePublisher
    {
        public IServiceScopeFactory ServiceScopeFactory { get; }

        public ITransactionAccessor TransactionAccessor { get; }

        protected IStorage Storage { get; }

        protected MessagePublisherBase(
            IServiceScopeFactory serviceScopeFactory)
        {
            ServiceScopeFactory = serviceScopeFactory;
            var provider = ServiceScopeFactory.CreateScope().ServiceProvider;
            TransactionAccessor = provider.GetRequiredService<ITransactionAccessor>();
            Storage = provider.GetService<IStorage>();
        }

        public Task PublishAsync<T>(T message)
            where T : class, IMessage
        {
            if (Storage == null)
            {
                SendAsync(message);
            }
            else
            {
                var transaction = (TransactionBase)TransactionAccessor.Transaction;
                Storage.StoreMessage(new MediumMessage(message), transaction?.DbTransaction);

                if (transaction == null)
                {
                    //未开启事务
                    SendAsync(message);
                }
                else
                {
                    if (transaction.AutoCommit)
                    {
                        TransactionAccessor.Transaction.Commit();
                        SendAsync(message);
                    }
                    else
                    {
                        transaction.AddMessage(message);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public abstract Task SendAsync<T>(T message)
            where T : class, IMessage;
    }
}
