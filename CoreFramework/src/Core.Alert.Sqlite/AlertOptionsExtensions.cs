using Core.Alert;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Alert.Sqlite
{
    public class AlertOptionsExtensions : IAlertOptionsExtensions
    {
        private readonly Action<AlertSqliteStorageOptions> _options;
        private readonly IConfiguration _configuration;

        public AlertOptionsExtensions(Action<AlertSqliteStorageOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public AlertOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                services.Configure(_options);
            }
            else if (_configuration != null)
            {
                services.Configure<AlertSqliteStorageOptions>(_configuration);
            }

            services.Replace(ServiceDescriptor.Singleton<IAlertStorageProvider, SqliteAlertStorageProvider>());
        }
    }
}
