namespace Core.EventBus.Mysql
{
    public class EventBusMysqlOptions
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DbConnectionStr { get; set; }

        /// <summary>
        /// 若数据库不存在table，即创建新的table
        /// </summary>
        public string TableName { get; set; } = "PublishMessage";
    }
}
