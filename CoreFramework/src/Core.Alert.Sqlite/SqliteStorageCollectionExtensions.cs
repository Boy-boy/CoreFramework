using Core.Alert;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqliteStorageCollectionExtensions
    {
        public static AlertOptions AddSqlite(
            this AlertOptions options,
            Action<Core.Alert.Sqlite.AlertSqliteStorageOptions> actionOptions)
        {
            options.AddExtensions(new Core.Alert.Sqlite.AlertOptionsExtensions(actionOptions));
            return options;
        }

        public static AlertOptions AddSqlite(
            this AlertOptions options,
            IConfiguration configuration)
        {
            options.AddExtensions(new Core.Alert.Sqlite.AlertOptionsExtensions(configuration));
            return options;
        }
    }
}
