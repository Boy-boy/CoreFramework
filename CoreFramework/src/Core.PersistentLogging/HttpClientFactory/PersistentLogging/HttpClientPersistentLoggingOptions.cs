using System.Collections.Generic;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public class HttpClientPersistentLoggingOptions
    {
        public Dictionary<string,List<string>> StorageSources { get; set; }
    }
}
