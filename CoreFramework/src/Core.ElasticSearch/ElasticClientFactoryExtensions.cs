using Nest;

namespace Core.ElasticSearch
{
    public static class ElasticClientFactoryExtensions
    {
        public static ElasticClient CreateClient(this IElasticClientFactory elasticClientFactory)
        {
            return elasticClientFactory.CreateClient(Microsoft.Extensions.Options.Options.DefaultName);
        }
    }
}
