namespace Core.EventBus.SqlServer
{
    public class EventBusSqlServerOptions
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DbConnectionStr { get; set; }

        /// <summary>
        /// 若数据库不存在schema，即创建新的schema
        /// </summary>
        public string DbSchema { get; set; } = "EventBus";

        /// <summary>
        /// 若数据库不存在table，即创建新的table
        /// </summary>
        public string TableName { get; set; } = "PublishMessage";
    }
}
