namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public class HttpClientPersistentLoggingOptions
    {
        /// <summary>
        /// 是否全局启用
        /// </summary>
        public bool GlobalEnable { get; set; } = false;

        /// <summary>
        /// 日志存储介质
        /// </summary>
        public List<string> StorageSources { get; set; } = new();
    }
}
