using Core.EventBus.Transaction;
using System.Threading.Tasks;
using Core.EventBus.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.EventBus.Integration
{
    public abstract class IntegrationMessagePublisherBase : IMessagePublisher
    {
        public IServiceProvider ServiceProvider { get; }

        public ITransactionAccessor TransactionAccessor { get; }

        protected IntegrationMessagePublisherBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            TransactionAccessor = serviceProvider.GetRequiredService<ITransactionAccessor>();
        }

        public virtual async Task PublishAsync<T>(T message)
            where T : class, IMessage
        {
            var transaction = (Transaction.Transaction)TransactionAccessor.Transaction;
            if (transaction != null)
            {
                //开启事务，表示使用发件箱模式
                var storage = ServiceProvider.GetRequiredService<IStorage>();
                storage.StoreMessage(new MediumMessage(message), transaction.DbTransaction);
                return;
            }
            await SendAsync(message);
        }

        public abstract Task SendAsync<T>(T message)
            where T : class, IMessage;
    }
}
