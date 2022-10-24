using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Permission.Storage
{
    public class InMemoryOptionsExtensions : IPermissionOptionsExtensions
    {
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton(typeof(IPermissionGrantsStorage),typeof(InMemoryPermissionGrantsStorage));
        }
    }
}
