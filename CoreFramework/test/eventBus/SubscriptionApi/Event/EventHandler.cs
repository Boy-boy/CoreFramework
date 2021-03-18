using Core.EventBus;
using System;
using System.Threading.Tasks;

namespace SubscriptionApi.Event
{
    public class EventHandler : IMessageHandler<CustomerEvent>
    {
        public Task HandAsync(CustomerEvent message)
        {
            Console.WriteLine(message.Id);
            return Task.CompletedTask;
        }
    }
}
