namespace Core.Permission.PostgreSql
{
    public class PermissionPostgreSqlOptions
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DbConnection { get; set; }

        /// <summary>
        /// 若数据库不存在schema，即创建新的schema
        /// </summary>
        public string DbSchema { get; set; } = "Permission";

        /// <summary>
        /// 若数据库不存在table，即创建新的table
        /// </summary>
        public string DbTable { get; set; } = "AccessRole";
    }
}
