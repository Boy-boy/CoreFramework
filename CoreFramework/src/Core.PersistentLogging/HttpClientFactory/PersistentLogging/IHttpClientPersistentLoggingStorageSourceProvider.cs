using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public interface IHttpClientPersistentLoggingStorageSourceProvider
    {
        Task<IHttpClientPersistentLoggingStorageSource> GetStorageSource(string name);

        Task<IEnumerable<IHttpClientPersistentLoggingStorageSource>> GetStorageSources();
    }
}
