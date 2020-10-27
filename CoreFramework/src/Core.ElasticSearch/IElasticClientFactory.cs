using Nest;

namespace Core.ElasticSearch
{
    public interface IElasticClientFactory
    {
        ElasticClient CreateClient(string name);
    }
}
