using System;
using Core.Configuration.MySql;

namespace Microsoft.Extensions.Configuration
{
    public static class MySqlConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddMySqlConfigure(this IConfigurationBuilder builder, Action<MySqlConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}
