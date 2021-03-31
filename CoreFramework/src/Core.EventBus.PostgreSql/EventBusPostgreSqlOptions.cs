namespace Core.EventBus.PostgreSql
{
    public class EventBusPostgreSqlOptions
    {
        public string ConnectionString { get; set; }

        public string PublishMessageTableName { get; set; } = "EventBusPublishMessage";
    }
}
