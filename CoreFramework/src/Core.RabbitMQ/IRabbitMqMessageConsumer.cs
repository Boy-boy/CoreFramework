using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Core.RabbitMQ
{
    public interface IRabbitMqMessageConsumer:IDisposable
    {
        Task BindAsync(string routingKey);

        Task UnbindAsync(string routingKey);

        bool HasRoutingKeyBindingQueue();

        void OnMessageReceived(Func<IModel, BasicDeliverEventArgs, Task> processEvent);
    }
}
