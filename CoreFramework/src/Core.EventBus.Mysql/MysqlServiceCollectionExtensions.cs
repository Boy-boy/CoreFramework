using Microsoft.Extensions.Configuration;
using System;

namespace Core.EventBus.Mysql
{
    public static class MysqlServiceCollectionExtensions
    {
        public static EventBusOptions AddMysql(this EventBusOptions options, Action<EventBusMysqlOptions> actionOptions)
        {
            if (actionOptions == null)
                throw new ArgumentNullException(nameof(actionOptions));
            options.AddExtensions(new EventBusOptionsExtensions(actionOptions));
            return options;
        }

        public static EventBusOptions AddMysql(this EventBusOptions options, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            options.AddExtensions(new EventBusOptionsExtensions(configuration));
            return options;
        }
    }
}
