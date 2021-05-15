using Core.Configuration.SqlServer;
using System;

namespace Microsoft.Extensions.Configuration
{
    public static class SqlServerConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddSqlServerConfigure(this IConfigurationBuilder builder, Action<SqlServerConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}
