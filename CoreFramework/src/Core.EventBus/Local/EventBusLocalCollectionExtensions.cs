using Core.EventBus.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Local
{
    public static class EventBusLocalCollectionExtensions
    {
        public static EventBusBuilder AddLocal(this EventBusBuilder eventBusBuilder)
        {
            eventBusBuilder.Services.AddSingleton<IEventBus, EventBusLocal>();
            return eventBusBuilder;
        }
    }
}
