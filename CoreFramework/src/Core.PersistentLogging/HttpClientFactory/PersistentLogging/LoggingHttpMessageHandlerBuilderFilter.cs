using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging
{
    public class LoggingHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly IHttpClientPersistentLoggingStorageSourceProvider _storageSourceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptionsMonitor<HttpClientPersistentLoggingOptions> _options;

        public LoggingHttpMessageHandlerBuilderFilter(IHttpClientPersistentLoggingStorageSourceProvider storageSourceProvider,
            ILoggerFactory loggerFactory,
            IOptionsMonitor<HttpClientPersistentLoggingOptions> options)
        {
            _storageSourceProvider = storageSourceProvider;
            _loggerFactory = loggerFactory;
            _options = options;
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            return builder =>
            {
                next!(builder);

                var option = _options.CurrentValue;
                var globalEnable = option.GlobalEnable;
                if (globalEnable)
                {
                    builder.AdditionalHandlers.Add(new PersistentLoggingHttpMessageHandler(_storageSourceProvider, _loggerFactory, _options));
                }
            };
        }
    }
}
