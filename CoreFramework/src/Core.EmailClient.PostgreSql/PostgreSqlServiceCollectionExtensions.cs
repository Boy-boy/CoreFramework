using Core.EmailClient;
using Core.EmailClient.PostgreSql;
using Core.EmailClient.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PostgreSqlServiceCollectionExtensions
    {
        public static IServiceCollection AddPostgreSql(this IServiceCollection services,
            Action<PostgreSqlEmailStorageOptions> options)
        {
            if (options == null)
                throw new AggregateException(nameof(options));
            services.TryAddSingleton<IEmailStorage, PostgreSqlStorage>();
            services.Configure(options);
            return services;
        }

        public static IServiceCollection AddPostgreSql(this IServiceCollection services,
            IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            services.TryAddSingleton<IEmailStorage, PostgreSqlStorage>();
            services.Configure<PostgreSqlEmailStorageOptions>(configuration);
            return services;
        }

        public static EmailClientOptions AddPostgreSql(this EmailClientOptions options, Action<PostgreSqlEmailStorageOptions> actionOptions)
        {
            options.AddExtensions(new EmailClientPostgreSqlOptionsExtensions(actionOptions));
            return options;
        }

        public static EmailClientOptions AddPostgreSql(this EmailClientOptions options, IConfiguration configuration)
        {
            options.AddExtensions(new EmailClientPostgreSqlOptionsExtensions(configuration));
            return options;
        }
    }
}
