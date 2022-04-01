using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Amazon.S3
{
    public class DefaultTypeClientFactory<TClient> : ITypeClientFactory<TClient>
    where TClient : class
    {
        private readonly Cache _cache;
        private readonly IServiceProvider _serviceProvider;

        public DefaultTypeClientFactory(Cache cache, IServiceProvider serviceProvider)
        {
            _cache = cache;
            _serviceProvider = serviceProvider;
        }
        public TClient CreateClient(AmazonS3Client adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            return (TClient)_cache.Activator(_serviceProvider, new object[] { adapter });
        }

        public class Cache
        {
            private static readonly Func<ObjectFactory> CreateActivator = () => ActivatorUtilities.CreateFactory(typeof(TClient), new[] { typeof(AmazonS3Client) });

            private ObjectFactory _activator;
            private bool _initialized;
            private object _lock;

            public ObjectFactory Activator => LazyInitializer.EnsureInitialized(
                ref _activator,
                ref _initialized,
                ref _lock,
                CreateActivator);
        }
    }
}
