namespace Core.RabbitMQ
{
    public class RabbitMqOptions
    {
        public RabbitMqConnectionConfigure Connection { get; set; }

        public RabbitMqOptions()
        {
            Connection = new RabbitMqConnectionConfigure();
        }
    }
}
