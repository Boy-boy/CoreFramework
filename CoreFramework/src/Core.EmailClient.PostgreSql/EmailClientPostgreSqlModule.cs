using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EmailClient.PostgreSql
{
    [DependsOn(typeof(CoreEmailClientModule))]
    public class EmailClientPostgreSqlModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public EmailClientPostgreSqlModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EmailClientOptions>(options =>
            {
                options.AddPostgreSql(Configuration.GetSection("EmailClient:Storage"));
            });
        }
    }
}
