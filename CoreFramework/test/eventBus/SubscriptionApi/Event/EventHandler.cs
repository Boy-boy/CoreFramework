using Core.EventBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubscriptionApi.Event
{
    public class EventHandler : IMessageHandler<CustomerEvent>
    {
        public Task HandAsync(CustomerEvent message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("handler"+message.Id);
            return Task.CompletedTask;
        }
    }
}
