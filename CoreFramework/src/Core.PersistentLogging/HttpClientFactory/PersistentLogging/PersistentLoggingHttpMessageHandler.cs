using Core.PersistentLogging.HttpClientFactory.PersistentLogging.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public class PersistentLoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly IHttpClientPersistentLoggingStorageSourceProvider _storageSourceProvider;
        private readonly IOptionsMonitor<HttpClientPersistentLoggingOptions> _options;
        private readonly ILogger _logger;

        public PersistentLoggingHttpMessageHandler(IHttpClientPersistentLoggingStorageSourceProvider storageSourceProvider,
            ILoggerFactory loggerFactory,
            IOptionsMonitor<HttpClientPersistentLoggingOptions> options)
        {
            _storageSourceProvider = storageSourceProvider;
            _options = options;
            _logger = loggerFactory.CreateLogger<PersistentLoggingHttpMessageHandler>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            Exception exception = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch (Exception e)
            {
                exception = e;
                _logger.LogError($"httpClient持久化日志发生错误，原因：{e.Message}", e);
                throw;
            }
            finally
            {
                var option = _options.CurrentValue;
                var storageSourceNames = option.StorageSources;

                foreach (var storageSourceName in storageSourceNames)
                {
                    var storageSource = await _storageSourceProvider.GetStorageSource(storageSourceName);
                    if (storageSource == null)
                        continue;
                    var model = new PersistentLoggingDto(new HttpRequestDto(request), new HttpResponseDto(response));

                    if (exception != null)
                    {
                        model.ActionExecutionException(exception);
                    }

                    await storageSource.AddLog(model);
                }
            }
        }
    }
}
