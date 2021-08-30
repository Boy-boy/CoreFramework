using System.Reflection;

namespace Core.EventBus.RabbitMQ
{
    public class RabbitMqConstants
    {
        public static string DefaultExchangeName = "event_bus_default_exchange";
        public static string DefaultQueueName = Assembly.GetEntryAssembly()?.GetName().Name;
    }
}
