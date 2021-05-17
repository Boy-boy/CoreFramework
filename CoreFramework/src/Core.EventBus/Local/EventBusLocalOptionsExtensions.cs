using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Local
{
    public class EventBusLocalOptionsExtensions : IEventBusOptionsExtensions
    {
        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<IMessagePublisher, LocalMessagePublisher>();
            services.AddSingleton<IMessageSubscribe, LocalMessageSubscribe>();
        }
    }
}
