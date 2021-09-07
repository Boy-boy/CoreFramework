using System;
using Core.Permission.PostgreSql;

namespace Core.Permission.Storage
{
    public static class PostgreSqlBuilderExtensions
    {
        public static PermissionOptions AddPostgreSql(this PermissionOptions options,Action<PermissionPostgreSqlOptions> actionOptions)
        {
            options.AddExtensions(new PostgreSqlOptionsExtensions(actionOptions));
            return options;
        }
    }
}
