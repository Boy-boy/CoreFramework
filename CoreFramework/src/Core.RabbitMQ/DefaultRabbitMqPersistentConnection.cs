using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Core.RabbitMQ
{
    public class DefaultRabbitMqPersistentConnection
       : IRabbitMqPersistentConnection
    {
        private readonly RabbitMqOptions _option;
        private readonly ILogger<DefaultRabbitMqPersistentConnection> _logger;
        private readonly int _retryCount;
        IConnection _connection;
        volatile bool _disposed;
        volatile bool _blocked;
        long _reconnectCount;


        readonly object _syncRoot = new();

        public DefaultRabbitMqPersistentConnection(IOptions<RabbitMqOptions> option,
            ILogger<DefaultRabbitMqPersistentConnection> logger)
        {
            _option = option.Value;
            _logger = logger;
            // 由 options 决定 Polly 退避次数;负数在 ValidateNumericLimits 已经拒绝,这里再做一次 max(0) 兜底防裸调。
            _retryCount = Math.Max(0, _option?.ConnectionRetryCount ?? 6);
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        /// <summary>broker 触发 flow-control 后是否还在 blocked 状态;HealthCheck / Metrics 消费。</summary>
        public bool IsBlocked => _blocked;

        /// <summary>累计后台重连次数(含成功与失败尝试),供 metrics 采样。</summary>
        public long ReconnectAttempts => Interlocked.Read(ref _reconnectCount);

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

            // 不进 _syncRoot：TryConnect 持有 _syncRoot 时可能正在跑 Polly 退避（最坏 ~126s）。
            // _disposed 是 volatile，TryConnect 出 Polly 后会自检 _disposed 并清理 orphan 连接（见 TryConnect 末尾）。
            // 这里只负责释放 dispose 调用前已经存在的连接。
            var conn = Interlocked.Exchange(ref _connection, null);
            if (conn != null)
            {
                try { conn.Dispose(); }
                catch (IOException ex) { _logger.LogCritical(ex.ToString()); }
            }
        }

        public bool TryConnect()
        {
            // dispose 之后再调用必须直接返回，否则后面 Polly 退避里仍会成功建连然后 _disposed 把 IsConnected 拉回 false，
            // 调用方看到失败、对象被 leak。
            if (_disposed) return false;

            if (IsConnected)
                return true;

            // 配置缺失时给出可读异常，而不是在 Polly 退避里反复抛 NRE 把日志刷爆 / 把启动炸掉。
            if (string.IsNullOrWhiteSpace(_option?.Connection?.HostName))
            {
                throw new InvalidOperationException(
                    "RabbitMQ HostName 未配置。请在 appsettings.json 的 RabbitMq:Connection:HostName 节点设置 broker 地址" +
                    "（单机：\"host\"，集群：\"host1;host2;host3\"）。");
            }

            _logger.LogInformation("RabbitMQ Client is trying to connect");
            lock (_syncRoot)
            {
                if (_disposed) return false;
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
                    // Polly 退避途中可能被 dispose；每次重试前都要 short-circuit，避免在 disposed 对象上再建连接。
                    if (_disposed || IsConnected)
                        return;

                    var connectionFactory = _option.Connection.ConnectionFactory;
                    var hostnames = _option.Connection.HostName.TrimEnd(';').Split(';');
                    // Handle Rabbit MQ Cluster.
                    _connection = hostnames.Length == 1
                        ? connectionFactory.CreateConnection()
                        : connectionFactory.CreateConnection(hostnames);
                });

                // policy.Execute 期间可能并发触发 Dispose：此时 Dispose 看到 _connection 还是 null，
                // 等 Execute 出来时连接才被赋上，外面已经不会再清理。这里兜底把 orphan 释放掉。
                if (_disposed)
                {
                    var orphan = _connection;
                    _connection = null;
                    if (orphan != null)
                    {
                        try { orphan.Dispose(); }
                        catch (Exception ex) { _logger.LogWarning(ex, "RabbitMQ orphan connection dispose failed after disposal race"); }
                    }
                    return false;
                }

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

        /// <summary>
        /// broker 触发 flow-control 时调用。注意：此时连接<b>仍然 open</b>，<see cref="IsConnected"/> 为 true，
        /// 因此不需要也不应该重连。等 broker 内存/磁盘恢复后会自动 <c>ConnectionUnblocked</c>。
        /// </summary>
        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _blocked = true;
            // 仅记录，不重连。调用 TryConnect 在此处是 no-op（IsConnected 已经是 true）且语义误导。
            _logger.LogWarning("RabbitMQ connection blocked by broker (flow-control): {Reason}", e.Reason);

            if (sender is IConnection c)
            {
                c.ConnectionUnblocked -= OnConnectionUnblocked;
                c.ConnectionUnblocked += OnConnectionUnblocked;
            }
        }

        private void OnConnectionUnblocked(object sender, EventArgs e)
        {
            _blocked = false;
            _logger.LogInformation("RabbitMQ connection unblocked, broker resumed accepting publishes");
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            // 不能在 client dispatcher 线程上同步等 Polly 几十秒退避，否则会拖垮 broker 心跳与其他回调。
            ReconnectInBackground();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            ReconnectInBackground();
        }

        /// <summary>
        /// 把重连甩到线程池。重连失败时只 log，避免 unobserved task 拖垮宿主。
        /// </summary>
        private void ReconnectInBackground()
        {
            Interlocked.Increment(ref _reconnectCount);
            _ = Task.Run(() =>
            {
                try
                {
                    TryConnect();
                }
                catch (Exception ex)
                {
                    // 重连失败不应让进程崩。后续 broker 操作触发的下一次 TryConnect 会再试。
                    _logger.LogError(ex, "RabbitMQ background reconnect failed");
                }
            });
        }
    }
}
