namespace Core.EmailClient.Mysql
{
    public class MysqlEmailStorageOptions
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DbConnection { get; set; }

        /// <summary>
        /// 若数据库不存在table，即创建新的table
        /// </summary>
        public string DbTable { get; set; }
    }
}
