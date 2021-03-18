using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    public class EventBusBuilder
    {
        public EventBusBuilder(IServiceCollection service)
        {
            Service = service;
        }
        public IServiceCollection Service { get; set; }
    }
}
