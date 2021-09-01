using Core.EventBus;
using System;
using System.Threading.Tasks;

namespace SubscriptionApi.Event
{
    public class EventHandler1 : IMessageHandler<CustomerEvent>
    {
        public Task HandAsync(CustomerEvent message)
        {
            Console.WriteLine(message.Id);
            return Task.CompletedTask;
        }
    }
}
