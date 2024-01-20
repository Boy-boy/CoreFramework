using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public class DefaultActionFilterPersistentLoggingStorageSourceProvider : IActionFilterPersistentLoggingStorageSourceProvider
    {
        private readonly IEnumerable<IActionFilterPersistentLoggingStorageSource> _dataSources;

        public DefaultActionFilterPersistentLoggingStorageSourceProvider(IEnumerable<IActionFilterPersistentLoggingStorageSource> dataSources)
        {
            _dataSources = dataSources ?? new List<IActionFilterPersistentLoggingStorageSource>();
        }

        public async Task<IActionFilterPersistentLoggingStorageSource> GetStorageSource(string name)
        {
            return await Task.FromResult(_dataSources.FirstOrDefault(p => p.Name == name));
        }

        public async Task<IEnumerable<IActionFilterPersistentLoggingStorageSource>> GetStorageSources()
        {
            return await Task.FromResult(_dataSources);
        }
    }
}
