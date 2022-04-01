using Microsoft.Extensions.DependencyInjection;

namespace Core.Amazon.S3
{
    public class DefaultAmazonS3Builder : IAmazonS3Builder
    {
        public DefaultAmazonS3Builder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}
