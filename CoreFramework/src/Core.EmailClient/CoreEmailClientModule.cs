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
            //TODO:兼容客户端注入EmailClientOptions
            var implementationInstances = context.Services
                .Where(p => p.ServiceType == typeof(IConfigureOptions<EmailClientOptions>))
                .Select(p => (IConfigureOptions<EmailClientOptions>)p.ImplementationInstance)
                .ToList();

            if (!implementationInstances.Any())
                return;

            var emailClientOptions = new EmailClientOptions();
            foreach (var implementationInstance in implementationInstances)
            {
                implementationInstance.Configure(emailClientOptions);
            }
            emailClientOptions.Configure(context.Services);
        }
    }
}
