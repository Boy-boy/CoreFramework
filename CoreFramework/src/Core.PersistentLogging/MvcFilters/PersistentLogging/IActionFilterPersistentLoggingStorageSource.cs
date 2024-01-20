using System.Threading.Tasks;
using Core.PersistentLogging.MvcFilters.PersistentLogging.Model;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public interface IActionFilterPersistentLoggingStorageSource
    {
        public string Name { get; set; }

        Task AddLog(PersistentLoggingDto dto);
    }
}
