using Core.EmailClient;
using Core.EmailClient.Mysql;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MysqlServiceCollectionExtensions
    {
        public static EmailClientOptions AddMysql(this EmailClientOptions options, Action<MysqlEmailStorageOptions> actionOptions)
        {
            options.AddExtensions(new EmailClientOptionsExtensions(actionOptions));
            return options;
        }

        public static EmailClientOptions AddMysql(this EmailClientOptions options, IConfiguration configuration)
        {
            options.AddExtensions(new EmailClientOptionsExtensions(configuration));
            return options;
        }
    }
}
