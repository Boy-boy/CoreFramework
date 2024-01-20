using System.Threading.Tasks;
using Core.PersistentLogging.HttpClientFactory.PersistentLogging.Model;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public interface IHttpClientPersistentLoggingStorageSource
    {
        public string Name { get; set; }

        Task AddLog(PersistentLoggingDto dto);
    }
}
