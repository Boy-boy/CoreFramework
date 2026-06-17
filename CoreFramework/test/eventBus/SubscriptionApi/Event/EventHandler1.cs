using Core.EventBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubscriptionApi.Event
{
    [MessageHandlerPriority(9)]
    public class EventHandler1 : IMessageHandler<CustomerEvent>
    {
        public Task HandAsync(CustomerEvent message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("handler1"+message.Id);
            return Task.CompletedTask;
        }
    }
}
