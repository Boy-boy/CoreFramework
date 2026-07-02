using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace Core.RabbitMQ
{
    /// <summary>
    /// RabbitMQ 连接层健康检查。<br/>
    /// - <see cref="IRabbitMqPersistentConnection.IsConnected"/> 为 false → Unhealthy;<br/>
    /// - blocked(broker flow-control)→ Degraded,发布会被 broker 挂起;<br/>
    /// - 已连接且未 blocked → Healthy。
    /// </summary>
    /// <remarks>
    /// 不主动 TryConnect,健康检查不应触发副作用(会拖住探针几十秒)。
    /// 需要"探活"语义时可以在 hosted service 里周期性 TryConnect,健康检查只上报当前状态。
    /// </remarks>
    public sealed class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly IRabbitMqPersistentConnection _connection;

        public RabbitMqHealthCheck(IRabbitMqPersistentConnection connection)
        {
            _connection = connection;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (!_connection.IsConnected)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "RabbitMQ 未连接",
                    data: new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["reconnectAttempts"] = _connection.ReconnectAttempts,
                    }));
            }

            if (_connection.IsBlocked)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "RabbitMQ broker 处于 flow-control blocked 状态,publish 会被挂起",
                    data: new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["reconnectAttempts"] = _connection.ReconnectAttempts,
                    }));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "RabbitMQ 连接正常",
                data: new System.Collections.Generic.Dictionary<string, object>
                {
                    ["reconnectAttempts"] = _connection.ReconnectAttempts,
                }));
        }
    }
}
