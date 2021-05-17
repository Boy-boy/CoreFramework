using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    public interface IEventBusOptionsExtensions
    {
        void AddServices(IServiceCollection services);
    }
}
