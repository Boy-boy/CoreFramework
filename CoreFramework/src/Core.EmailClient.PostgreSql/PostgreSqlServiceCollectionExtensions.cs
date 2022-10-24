using Core.EmailClient;
using Core.EmailClient.PostgreSql;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PostgreSqlServiceCollectionExtensions
    {
        public static EmailClientOptions AddPostgreSql(this EmailClientOptions options, Action<PostgreSqlEmailStorageOptions> actionOptions)
        {
            options.AddExtensions(new EmailClientOptionsExtensions(actionOptions));
            return options;
        }

        public static EmailClientOptions AddPostgreSql(this EmailClientOptions options, IConfiguration configuration)
        {
            options.AddExtensions(new EmailClientOptionsExtensions(configuration));
            return options;
        }
    }
}
