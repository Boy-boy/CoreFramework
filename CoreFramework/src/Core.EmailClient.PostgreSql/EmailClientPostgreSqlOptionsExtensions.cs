using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EmailClient.PostgreSql
{
    public class EmailClientPostgreSqlOptionsExtensions : IEmailClientOptionsExtensions
    {
        private readonly Action<PostgreSqlEmailStorageOptions> _options;
        private readonly IConfiguration _configuration;

        public EmailClientPostgreSqlOptionsExtensions(Action<PostgreSqlEmailStorageOptions> options)
        {
            _options = options;
        }
        public EmailClientPostgreSqlOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                services.AddPostgreSql(_options);
            }
            else if (_configuration != null)
            {
                services.AddPostgreSql(_configuration);
            }
        }
    }
}
