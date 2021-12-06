using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;
using Polly;

namespace Core.RabbitMQ
{
    public class DefaultRabbitMqPersistentConnection
       : IRabbitMqPersistentConnection
    {
        private readonly RabbitMqOptions _option;
        private readonly ILogger<DefaultRabbitMqPersistentConnection> _logger;
        private readonly int _retryCount = 6;
        IConnection _connection;
        bool _disposed;


        readonly object _syncRoot = new();

        public DefaultRabbitMqPersistentConnection(IOptions<RabbitMqOptions> option,
            ILogger<DefaultRabbitMqPersistentConnection> logger)
        {
            _option = option.Value;
            _logger = logger;
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection?.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            if (IsConnected)
                return true;

            _logger.LogInformation("RabbitMQ Client is trying to connect");
            lock (_syncRoot)
            {
                if (IsConnected)
                    return true;

                var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                });

                policy.Execute(() =>
                {
                    if (IsConnected)
                        return;

                    var connectionFactory = _option.Connection.ConnectionFactory;
                    var hostnames = _option.Connection.HostName.TrimEnd(';').Split(';');
                    // Handle Rabbit MQ Cluster.
                    _connection = hostnames.Length == 1
                        ? connectionFactory.CreateConnection()
                        : connectionFactory.CreateConnection(hostnames);
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to events", _connection.Endpoint.HostName);

                    return true;
                }
                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                return false;
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on blocked. Trying to re-connect...");

            TryConnect();
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }
    }
}
