using Amazon.S3;

namespace Core.Amazon.S3
{
    public static class AmazonS3ClientFactoryExtensions
    {
        public static AmazonS3Client CreateClient(this IAmazonS3ClientFactory clientFactory)
        {
            return clientFactory.CreateClient(Microsoft.Extensions.Options.Options.DefaultName);
        }
    }
}
