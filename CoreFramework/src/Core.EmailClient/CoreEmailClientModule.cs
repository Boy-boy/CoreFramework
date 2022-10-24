using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            context.Services.Configure<EmailClientOptions>(Configuration.GetSection("EmailClient"));
            context.Services.AddEmailClient(_ => { });
        }

        public override void PostConfigureServices(ServiceCollectionContext context)
        {
            var serviceProvider = context.Services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<EmailClientOptions>>().Value;
            options.Configure(context.Services);
        }
    }
}
