using Microsoft.Extensions.DependencyInjection;

namespace Core.Amazon.S3
{
    public interface IAmazonS3Builder
    {
        string Name { get; }

        IServiceCollection Services { get; }
    }
}
