using Core.EventBus.Abstraction;
using System;
using System.Threading.Tasks;

namespace SubscriptionApi.Event
{
    public class EventHandler : IIntegrationEventHandler<CustomerEvent>
    {
        public Task HandleAsync(CustomerEvent @event)
        {
           Console.WriteLine(@event.Id);
           return Task.CompletedTask;
        }
    }
}
