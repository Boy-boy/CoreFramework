using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Configuration
{
    public class CoreConfigurationModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreConfigurationModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddDbConfiguration();
        }
    }
}
