using RabbitMQ.Client;
using System;

namespace Core.RabbitMQ
{
    public interface IRabbitMqPersistentConnection
        : IDisposable
    {
        bool IsConnected { get; }

        /// <summary>broker 是否处于 flow-control blocked 状态;true 时 IsConnected 仍为 true 但 publish 会被 broker 挂起。</summary>
        bool IsBlocked { get; }

        /// <summary>累计后台重连尝试次数,含成功与失败;metrics 采样用。</summary>
        long ReconnectAttempts { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}
