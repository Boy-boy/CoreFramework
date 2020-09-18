using RabbitMQ.Client;
using System.Collections.Generic;

namespace Core.RabbitMQ
{
    public class RabbitMqExchangeDeclareConfigure
    {
        public string ExchangeName { get; }

        public string Type { get; }

        public bool Durable { get; set; }

        public bool AutoDelete { get; set; }

        public IDictionary<string, object> Arguments { get; }

        public RabbitMqExchangeDeclareConfigure(
            string exchangeName,
            string type,
            bool durable = false,
            bool autoDelete = false,
            Dictionary<string, object> arguments = null)
        {
            ExchangeName = exchangeName;
            Type = type;
            Durable = durable;
            AutoDelete = autoDelete;
            Arguments = arguments ?? new Dictionary<string, object>();
        }

        public virtual void Declare(IModel channel)
        {
            channel.ExchangeDeclare(
               exchange: ExchangeName,
               type: Type,
               durable: Durable,
               autoDelete: AutoDelete,
               arguments: Arguments);
        }
    }
}
