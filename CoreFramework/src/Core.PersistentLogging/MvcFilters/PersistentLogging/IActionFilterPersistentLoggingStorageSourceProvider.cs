namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public interface IActionFilterPersistentLoggingStorageSourceProvider
    {
        Task<IActionFilterPersistentLoggingStorageSource> GetStorageSource(string name);

        Task<IEnumerable<IActionFilterPersistentLoggingStorageSource>> GetStorageSources();
    }
}
