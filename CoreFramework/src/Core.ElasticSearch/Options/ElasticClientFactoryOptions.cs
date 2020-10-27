using System;
using System.Threading;

namespace Core.ElasticSearch.Options
{
    public class ElasticClientFactoryOptions
    {
        internal static readonly TimeSpan MinimumElasticClientLifeTime = TimeSpan.FromHours(1.0);
        private TimeSpan _elasticClientLifeTime = TimeSpan.FromHours(24.0);

        public TimeSpan ElasticClientLifeTime
        {
            get => _elasticClientLifeTime;
            set
            {
                if (value != Timeout.InfiniteTimeSpan && value < MinimumElasticClientLifeTime)
                    throw new ArgumentException(nameof(value));
                _elasticClientLifeTime = value;
            }
        }
        public string UserName { get; set; }

        public string PassWord { get; set; } 

        public string[] Urls { get; set; }

        public string DefaultIndex { get; set; } = "elastic_search_default_index";

    }
}
