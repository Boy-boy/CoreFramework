using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    public class EventBusBuilder
    {
        public EventBusBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public virtual IServiceCollection Services { get; }
    }
}
