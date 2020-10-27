using Microsoft.Extensions.DependencyInjection;

namespace Core.ElasticSearch
{
    public class DefaultElasticClientBuilder
    {
        public DefaultElasticClientBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}
