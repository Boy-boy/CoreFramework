using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EmailClient.Mysql
{
    public class EmailClientMysqlOptionsExtensions : IEmailClientOptionsExtensions
    {
        private readonly Action<MysqlEmailStorageOptions> _options;
        private readonly IConfiguration _configuration;

        public EmailClientMysqlOptionsExtensions(Action<MysqlEmailStorageOptions> options)
        {
            _options = options;
        }
        public EmailClientMysqlOptionsExtensions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddServices(IServiceCollection services)
        {
            if (_options != null)
            {
                services.AddMysql(_options);
            }
            else if (_configuration != null)
            {
                services.AddMysql(_configuration);
            }
        }
    }
}
