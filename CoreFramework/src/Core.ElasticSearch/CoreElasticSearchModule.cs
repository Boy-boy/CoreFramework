using Core.Modularity;

namespace Core.ElasticSearch
{
    public class CoreElasticSearchModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddElasticClientFactory();
        }
    }
}
