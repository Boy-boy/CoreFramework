using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EmailClient.Mysql
{
    [DependsOn(typeof(CoreEmailClientModule))]
    public class EmailClientMysqlModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public EmailClientMysqlModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddMysql(Configuration.GetSection("EmailClient:Storage"));
        }
    }
}
