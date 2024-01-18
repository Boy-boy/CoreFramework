using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.PersistentLogging.HttpClientFactory.PersistentLogging.Model;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public class LoggingActionFilterPersistentLoggingStorageSource : IHttpClientPersistentLoggingStorageSource
    {
        private readonly ILogger<LoggingActionFilterPersistentLoggingStorageSource> _logger;

        public LoggingActionFilterPersistentLoggingStorageSource(ILogger<LoggingActionFilterPersistentLoggingStorageSource> logger)
        {
            _logger = logger;
        }

        public string Name { get; set; } = "Logging";

        public async Task AddLog(PersistentLoggingDto dto)
        {
            if (dto == null)
                return;

            var logging = JsonSerializer.Serialize(dto);
            _logger.LogInformation($"HttpClient请求-持久化请求日志信息：【{logging}】");
            await Task.CompletedTask;
        }
    }
}
