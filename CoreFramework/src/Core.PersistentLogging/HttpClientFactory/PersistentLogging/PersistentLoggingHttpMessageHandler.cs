using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.PersistentLogging.HttpClientFactory.PersistentLogging.Model;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public class PersistentLoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly IHttpClientPersistentLoggingStorageSourceProvider _storageSourceProvider;
        private readonly IOptionsMonitor<HttpClientPersistentLoggingOptions> _options;
        private readonly string _name;
        private readonly ILogger _logger;

        public PersistentLoggingHttpMessageHandler(IHttpClientPersistentLoggingStorageSourceProvider storageSourceProvider,
            ILoggerFactory loggerFactory,
            IOptionsMonitor<HttpClientPersistentLoggingOptions> options,
            string name)
        {
            _storageSourceProvider = storageSourceProvider;
            _options = options;
            _name = name;
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
                var option = _options.Get(_name);
                if (option.StorageSources.TryGetValue(_name, out var storageSourceNames))
                {
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
}
