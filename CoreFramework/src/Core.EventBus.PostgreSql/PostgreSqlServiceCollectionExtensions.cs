using Microsoft.Extensions.Configuration;
using System;

namespace Core.EventBus.PostgreSql
{
    public static class PostgreSqlServiceCollectionExtensions
    {
        public static EventBusOptions AddPostgreSql(this EventBusOptions options, Action<EventBusPostgreSqlOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));
            options.AddExtensions(new EventBusOptionsExtensions(actionOptions));
            return options;
        }

        public static EventBusOptions AddPostgreSql(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            options.AddExtensions(new EventBusOptionsExtensions(configuration));
            return options;
        }
    }
}
