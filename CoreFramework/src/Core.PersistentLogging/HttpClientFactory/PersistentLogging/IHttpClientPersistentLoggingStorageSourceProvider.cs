namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public interface IHttpClientPersistentLoggingStorageSourceProvider
    {
        Task<IHttpClientPersistentLoggingStorageSource> GetStorageSource(string name);

        Task<IEnumerable<IHttpClientPersistentLoggingStorageSource>> GetStorageSources();
    }
}
