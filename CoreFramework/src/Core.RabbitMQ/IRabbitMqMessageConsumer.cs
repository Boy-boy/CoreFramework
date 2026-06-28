using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Core.RabbitMQ
{
    public interface IRabbitMqMessageConsumer : IDisposable
    {
        /// <summary>把一个 routing key 绑定到本 consumer 的 queue。幂等。</summary>
        Task BindAsync(string routingKey);

        /// <summary>解除一个 routing key 与本 consumer queue 的绑定。幂等。</summary>
        Task UnbindAsync(string routingKey);

        /// <summary>是否还有 routing key 绑定在本 queue 上。绑定集为空时上层会释放本 consumer。</summary>
        bool HasAnyRoutingKey();

        /// <summary>注册消息处理回调。回调内抛出异常由 consumer 按 <see cref="RabbitMqOptions.FailureBehavior"/> 决定 ack/nack。</summary>
        void OnMessageReceived(Func<IModel, BasicDeliverEventArgs, Task> processEvent);
    }
}
