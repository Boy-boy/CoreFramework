namespace Core.EventBus.Mysql
{
    public class EventBusMysqlOptions
    {
        public string ConnectionString { get; set; }

        public string PublishMessageTableName { get; set; } = "EventBusPublishMessage";
    }
}
