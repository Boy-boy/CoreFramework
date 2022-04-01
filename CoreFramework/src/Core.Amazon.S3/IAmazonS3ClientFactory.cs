using Amazon.S3;

namespace Core.Amazon.S3
{
    public interface IAmazonS3ClientFactory
    {
        AmazonS3Client CreateClient(string clientName);
    }
}
