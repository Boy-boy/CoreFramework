using Amazon.S3;

namespace Core.Amazon.S3
{
    public interface ITypeClientFactory<out TClient>
    where TClient : class
    {
        TClient CreateClient(AmazonS3Client adapter);
    }
}
