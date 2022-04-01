using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Amazon.S3
{
    public class DefaultAmazonS3ClientFactory : IAmazonS3ClientFactory
    {
        private readonly IOptionsMonitor<AmazonS3Options> _options;
        private readonly IServiceProvider _serviceProvider;

        public DefaultAmazonS3ClientFactory(IOptionsMonitor<AmazonS3Options> options,
            IServiceProvider serviceProvider)
        {
            _options = options;
            _serviceProvider = serviceProvider;
        }

        public AmazonS3Client CreateClient(string clientName)
        {
            var awsS3Options = _options.Get(clientName);
            var client = new AmazonS3Client(awsS3Options.UserName, awsS3Options.Password, new AmazonS3Config
            {
                ServiceURL = awsS3Options.ServiceURL,
                MaxErrorRetry = awsS3Options.RetryCount,
                UseHttp = awsS3Options.UseHttp,
                ForcePathStyle = awsS3Options.ForcePathStyle,
                Timeout = awsS3Options.Timeout,
                SignatureVersion = awsS3Options.SignatureVersion,
                HttpClientCacheSize = 1,
                CacheHttpClient = true,
                HttpClientFactory = ActivatorUtilities.CreateInstance<AmazonS3ClientHttpClientFactory>(_serviceProvider)
            });
            return client;
        }
    }

    public class AmazonS3ClientHttpClientFactory : HttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AmazonS3ClientHttpClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public override HttpClient CreateHttpClient(IClientConfig clientConfig)
        {
            return _httpClientFactory.CreateClient(AmazonS3ClientHttpClientNameConstants.AmazonS3ClientHttpClientName);
        }
    }
}