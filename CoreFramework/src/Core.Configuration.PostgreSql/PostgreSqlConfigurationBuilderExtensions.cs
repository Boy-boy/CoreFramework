using Core.Configuration.PostgreSql;
using System;

namespace Microsoft.Extensions.Configuration
{
    public static class PostgreSqlConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddPostgreSqlConfigure(this IConfigurationBuilder builder, Action<PostgreSqlConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}
