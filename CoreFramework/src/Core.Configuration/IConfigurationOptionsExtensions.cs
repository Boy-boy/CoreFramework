using Microsoft.Extensions.DependencyInjection;

namespace Core.Configuration
{
    public interface IConfigurationOptionsExtensions
    {
        void AddServices(IServiceCollection services);
    }
}
