using Microsoft.Extensions.Configuration;
using System;

namespace Core.EventBus.SqlServer
{
    public static class SqlServerServiceCollectionExtensions
    {
        public static EventBusOptions AddSqlServer(this EventBusOptions options, Action<EventBusSqlServerOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));
            options.AddExtensions(new EventBusOptionsExtensions(actionOptions));
            return options;
        }

        public static EventBusOptions AddSqlServer(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            options.AddExtensions(new EventBusOptionsExtensions(configuration));
            return options;
        }
    }
}
