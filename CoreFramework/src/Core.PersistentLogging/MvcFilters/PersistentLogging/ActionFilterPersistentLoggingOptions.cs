namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public class ActionFilterPersistentLoggingOptions
    {
        /// <summary>
        /// 日志存储介质
        /// </summary>
        public List<string> StorageSources { get; set; } = new();
    }
}
