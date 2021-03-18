namespace Core.EventBus.SqlServer
{
    public class EventBusSqlServerOptions
    {
        public string ConnectionString { get; set; }

        public string PublishMessageTableName { get; set; } = "EventBusPublishMessage";
    }
}
