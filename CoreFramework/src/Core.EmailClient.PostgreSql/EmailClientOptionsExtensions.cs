using Core.EmailClient.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EmailClient.PostgreSql
{
    public class EmailClientOptionsExtensions : IEmailClientOptionsExtensions
    {
        private readonly Action<PostgreSqlEmailStorageOptions> _options;
        private readonly IConfiguration _configuration;

        public EmailClientOptionsExtensions(Action<PostgreSqlEmailStorageOptions> options)
        {
            _options = options ?? throw new AggregateException(nameof(options));
        }
        public EmailClientOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new AggregateException(nameof(configuration));
        }

        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                services.Configure(_options);
                AddCore(services);
            }
            else if (_configuration != null)
            {
                services.Configure<PostgreSqlEmailStorageOptions>(_configuration);
                AddCore(services);
            }
        }

        public void AddCore(IServiceCollection services)
        {
            services.TryAddSingleton<IEmailStorage, PostgreSqlStorage>();
        }
    }
}
