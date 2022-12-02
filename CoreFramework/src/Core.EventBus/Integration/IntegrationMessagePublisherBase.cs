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

        protected IStorage Storage { get; }

        protected IntegrationMessagePublisherBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            TransactionAccessor = serviceProvider.GetRequiredService<ITransactionAccessor>();
            Storage = serviceProvider.GetService<IStorage>();
        }

        public virtual async Task PublishAsync<T>(T message)
            where T : class, IMessage
        {
            var transaction = (TransactionBase)TransactionAccessor.Transaction;
            Storage?.StoreMessage(new MediumMessage(message), transaction?.DbTransaction);

            if (transaction == null)
            {
                await SendAsync(message);
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
