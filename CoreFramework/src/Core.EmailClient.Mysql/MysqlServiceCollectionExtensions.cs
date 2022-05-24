using Core.EmailClient;
using Core.EmailClient.Mysql;
using Core.EmailClient.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MysqlServiceCollectionExtensions
    {
        public static IServiceCollection AddMysql(this IServiceCollection services,
            Action<MysqlEmailStorageOptions> options)
        {
            if (options == null)
                throw new AggregateException(nameof(options));
            services.TryAddSingleton<IEmailStorage, MysqlStorage>();
            services.Configure(options);
            return services;
        }

        public static IServiceCollection AddMysql(this IServiceCollection services,
            IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            services.TryAddSingleton<IEmailStorage, MysqlStorage>();
            services.Configure<MysqlEmailStorageOptions>(configuration);
            return services;
        }

        public static EmailClientOptions AddMysql(this EmailClientOptions options, Action<MysqlEmailStorageOptions> actionOptions)
        {
            options.AddExtensions(new EmailClientMysqlOptionsExtensions(actionOptions));
            return options;
        }

        public static EmailClientOptions AddMysql(this EmailClientOptions options, IConfiguration configuration)
        {
            options.AddExtensions(new EmailClientMysqlOptionsExtensions(configuration));
            return options;
        }
    }
}
