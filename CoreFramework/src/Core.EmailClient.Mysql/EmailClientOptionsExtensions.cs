using Core.EmailClient.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EmailClient.Mysql
{
    public class EmailClientOptionsExtensions : IEmailClientOptionsExtensions
    {
        private readonly Action<MysqlEmailStorageOptions> _options;
        private readonly IConfiguration _configuration;

        public EmailClientOptionsExtensions(Action<MysqlEmailStorageOptions> options)
        {
            _options = options ?? throw new AggregateException(nameof(options));
        }
        public EmailClientOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
                services.Configure<MysqlEmailStorageOptions>(_configuration);
                AddCore(services);
            }
        }

        public void AddCore(IServiceCollection services)
        {
            services.TryAddSingleton<IEmailStorage, MysqlStorage>();
        }
    }
}
