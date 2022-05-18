using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Application
{
    public class CoreApplicationModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddApplication();
        }
    }
}
