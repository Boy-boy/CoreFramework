using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.PersistentLogging.MvcFilters.PersistentLogging.Model;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public class LoggingActionFilterPersistentLoggingStorageSource : IActionFilterPersistentLoggingStorageSource
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
            _logger.LogInformation($"请求接口-持久化接口日志信息：【{logging}】");
            await Task.CompletedTask;
        }
    }
}
