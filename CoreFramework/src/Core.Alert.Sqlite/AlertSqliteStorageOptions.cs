namespace Core.Alert.Sqlite
{
    /// <summary>
    /// SQLite 存储配置。
    /// </summary>
    public class AlertSqliteStorageOptions
    {
        /// <summary>
        /// SQLite 连接串，例如 "Data Source=alert.db"。
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
    }
}
