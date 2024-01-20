using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public class DefaultHttpClientPersistentLoggingStorageSourceProvider : IHttpClientPersistentLoggingStorageSourceProvider
    {
        private readonly IEnumerable<IHttpClientPersistentLoggingStorageSource> _dataSources;

        public DefaultHttpClientPersistentLoggingStorageSourceProvider(IEnumerable<IHttpClientPersistentLoggingStorageSource> dataSources)
        {
            _dataSources = dataSources ?? new List<IHttpClientPersistentLoggingStorageSource>();
        }

        public async Task<IHttpClientPersistentLoggingStorageSource> GetStorageSource(string name)
        {
            return await Task.FromResult(_dataSources.FirstOrDefault(p => p.Name == name));
        }

        public async Task<IEnumerable<IHttpClientPersistentLoggingStorageSource>> GetStorageSources()
        {
            return await Task.FromResult(_dataSources);
        }
    }
}
