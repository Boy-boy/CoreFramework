using Core.ElasticSearch.Options;
using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.ElasticSearch
{
    public class CoreElasticSearchModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreElasticSearchModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<ElasticClientFactoryOptions>(Configuration.GetSection("ElasticSearch"));
            context.Services.AddElasticClientFactory();
        }
    }
}
