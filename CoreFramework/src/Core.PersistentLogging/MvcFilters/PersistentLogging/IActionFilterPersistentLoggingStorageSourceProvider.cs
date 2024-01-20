using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public interface IActionFilterPersistentLoggingStorageSourceProvider
    {
        Task<IActionFilterPersistentLoggingStorageSource> GetStorageSource(string name);

        Task<IEnumerable<IActionFilterPersistentLoggingStorageSource>> GetStorageSources();
    }
}
