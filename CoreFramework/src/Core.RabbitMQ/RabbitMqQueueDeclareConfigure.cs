using RabbitMQ.Client;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.RabbitMQ
{
    public class RabbitMqQueueDeclareConfigure
    {
        [Required]
        public string QueueName { get; }

        public bool Durable { get; set; }

        public bool Exclusive { get; set; }

        public bool AutoDelete { get; set; }

        public IDictionary<string, object> Arguments { get; }

        public RabbitMqQueueDeclareConfigure(
            string queueName,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            Dictionary<string, object> arguments = null)
        {
            QueueName = queueName;
            Durable = durable;
            Exclusive = exclusive;
            AutoDelete = autoDelete;
            Arguments = arguments ?? new Dictionary<string, object>();
        }

        public virtual QueueDeclareOk Declare(IModel channel)
        {
            return channel.QueueDeclare(
                queue: QueueName,
                durable: Durable,
                exclusive: Exclusive,
                autoDelete: AutoDelete,
                arguments: Arguments
            );
        }
    }
}
