using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Core.ElasticSearch.Options;
using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using Nest;


namespace Core.ElasticSearch
{
    public class ElasticClientFactory : IElasticClientFactory, IDisposable
    {
        private readonly IOptionsMonitor<ElasticClientFactoryOptions> _options;
        private readonly TimerCallback _expiryCallback;
        private readonly ConcurrentDictionary<string, Lazy<ElasticClientTrackingEntry>> _elasticClients;

        private readonly string _defaultName = Microsoft.Extensions.Options.Options.DefaultName;

        public ElasticClientFactory(IOptionsMonitor<ElasticClientFactoryOptions> options)
        {
            _options = options;
            _elasticClients = new ConcurrentDictionary<string, Lazy<ElasticClientTrackingEntry>>(StringComparer.Ordinal);
            _expiryCallback = ExpiryTimer_Tick;
            _options.OnChange(ElasticClientFactoryOnChange);
        }

        private void ElasticClientFactoryOnChange(ElasticClientFactoryOptions option, string name)
        {
            if (_elasticClients.TryRemove(name, out var clientTrackingEntry))
            {
                clientTrackingEntry.Value.StopExpiryTimer();
            }
            CreateClient(name);
        }

        /// <summary>
        /// 获取ElasticClient
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ElasticClient CreateClient(string name = null)
        {
            name = name ?? _defaultName;
            var elasticClientTrackingEntry = _elasticClients
                .GetOrAdd(name,
                    new Lazy<ElasticClientTrackingEntry>(() => ElasticClientConnectionSettingsTrackingEntry(name), LazyThreadSafetyMode.ExecutionAndPublication));
            elasticClientTrackingEntry.Value.StartExpiryTimer(_expiryCallback);
            return elasticClientTrackingEntry.Value.ElasticClient;
        }

        private ElasticClientTrackingEntry ElasticClientConnectionSettingsTrackingEntry(string name)
        {
            var option = _options.Get(name);
            var uris = option.Urls.Select(h => new Uri(h)).ToArray();
            var connectionPool = new StaticConnectionPool(uris);

            var setting = new ConnectionSettings(connectionPool);
            setting.BasicAuthentication(option.UserName, option.PassWord);
            setting.DefaultIndex(option.DefaultIndex);
            setting.DisableDirectStreaming();
            setting.DefaultFieldNameInferrer(fieldName => fieldName);

            var elasticClient = new ElasticClient(setting);
            var elasticClientTrackingEntry = new ElasticClientTrackingEntry(name, elasticClient, option.ElasticClientLifeTime);
            return elasticClientTrackingEntry;
        }

        private void ExpiryTimer_Tick(object state)
        {
            var other = (ElasticClientTrackingEntry)state;
            _elasticClients.TryRemove(other.Name, out _);
        }

        public void Dispose()
        {
            _elasticClients.Clear();
        }
    }
}
