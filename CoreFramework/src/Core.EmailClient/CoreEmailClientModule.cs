using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EmailClient
{
    public class CoreEmailClientModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreEmailClientModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddEmailClient(_ => { });
        }
    }
}
