using RabbitMQ.Client;
using System;
using System.Net.Security;

namespace Core.RabbitMQ
{
    /// <summary>
    /// RabbitMQ 连接层配置。字段划分:<br/>
    /// - 基础(HostName/Port/User/Password/VirtualHost)<br/>
    /// - TLS(<see cref="Ssl"/>)<br/>
    /// - 会话参数(<see cref="RequestedHeartbeat"/> / <see cref="RequestedConnectionTimeout"/> / <see cref="SocketReadTimeout"/> / <see cref="SocketWriteTimeout"/>)<br/>
    /// - 客户端标识(<see cref="ClientProvidedName"/>,broker UI 里定位实例用)<br/>
    /// - Recovery(<see cref="AutomaticRecoveryEnabled"/> / <see cref="NetworkRecoveryInterval"/>,是否走官方自动恢复)<br/>
    /// </summary>
    /// <remarks>
    /// 默认值与旧版本保持一致(TLS 关、AutomaticRecovery 关、Heartbeat 60s),已有部署升级零迁移。
    /// AutomaticRecoveryEnabled=false 时,连接/channel 层重连完全由 <see cref="DefaultRabbitMqPersistentConnection"/>
    /// + <see cref="DefaultRabbitMqMessageConsumer"/> 里的自实现机制负责;true 时把恢复交给官方 client,
    /// 上层重连逻辑仍安全(IsConnected + 定时探活),但可以省掉部分重复工作。
    /// </remarks>
    public class RabbitMqConnectionConfigure
    {
        public string HostName { get; set; }

        public int Port { get; set; } = -1;

        public string UserName { get; set; }

        public string Password { get; set; }

        public string VirtualHost { get; set; }

        /// <summary>
        /// broker UI 上显示的 connection name;为空则不设,broker 会用一段 &lt;客户端 IP&gt;:&lt;端口&gt; 兜底,
        /// 生产多实例部署强烈建议设成 hostname + pid 便于定位。
        /// </summary>
        public string ClientProvidedName { get; set; }

        /// <summary>心跳周期,秒;弱网 / NAT / K8s network policy 后必须能显式指定。默认 60s,和 broker 官方一致。</summary>
        public ushort RequestedHeartbeat { get; set; } = 60;

        /// <summary>连接超时,毫秒;默认 30s。</summary>
        public int RequestedConnectionTimeout { get; set; } = 30_000;

        /// <summary>socket read timeout,毫秒;默认 30s,单次协议帧读取超时,不影响业务级 confirm 等待。</summary>
        public int SocketReadTimeout { get; set; } = 30_000;

        /// <summary>socket write timeout,毫秒;默认 30s。</summary>
        public int SocketWriteTimeout { get; set; } = 30_000;

        /// <summary>
        /// 是否启用 RabbitMQ.Client 内置的 automatic recovery(topology + 连接)。默认 false,
        /// 以保留和历史一致的行为(靠本项目自实现的 Polly 重连 + consumer timer 探活)。
        /// 打开后官方会在断线时按 <see cref="NetworkRecoveryInterval"/> 自动重连并恢复 topology,
        /// 上层机制仍然安全,可作为双保险,但会产生额外日志。
        /// </summary>
        public bool AutomaticRecoveryEnabled { get; set; }

        /// <summary>官方 automatic recovery 的重试间隔,秒;仅在 <see cref="AutomaticRecoveryEnabled"/>=true 时生效。默认 5。</summary>
        public int NetworkRecoveryInterval { get; set; } = 5;

        /// <summary>TLS 配置;为空对象且 <see cref="RabbitMqSslConfigure.Enabled"/>=false(默认)时,不启用 TLS,和原实现一致。</summary>
        public RabbitMqSslConfigure Ssl { get; set; } = new();

        public ConnectionFactory ConnectionFactory
        {
            get
            {
                var connectionFactory = new ConnectionFactory
                {
                    HostName = HostName,
                    UserName = UserName,
                    Password = Password,
                    DispatchConsumersAsync = true,
                    RequestedHeartbeat = TimeSpan.FromSeconds(RequestedHeartbeat),
                    RequestedConnectionTimeout = TimeSpan.FromMilliseconds(RequestedConnectionTimeout),
                    SocketReadTimeout = TimeSpan.FromMilliseconds(SocketReadTimeout),
                    SocketWriteTimeout = TimeSpan.FromMilliseconds(SocketWriteTimeout),
                    AutomaticRecoveryEnabled = AutomaticRecoveryEnabled,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(NetworkRecoveryInterval),
                };

                // Port 默认 -1 表示由 broker 端按协议自选(non-TLS 5672 / TLS 5671);业务传 0 或负数一律视作"用默认"。
                if (Port > 0)
                    connectionFactory.Port = Port;

                if (!string.IsNullOrWhiteSpace(VirtualHost))
                    connectionFactory.VirtualHost = VirtualHost;

                if (!string.IsNullOrWhiteSpace(ClientProvidedName))
                    connectionFactory.ClientProvidedName = ClientProvidedName;

                if (Ssl != null && Ssl.Enabled)
                {
                    connectionFactory.Ssl.Enabled = true;
                    // ServerName 不设时 broker 端 TLS 验证会因为 SNI 不匹配而失败:默认回退到 HostName
                    connectionFactory.Ssl.ServerName = string.IsNullOrWhiteSpace(Ssl.ServerName) ? HostName : Ssl.ServerName;
                    if (!string.IsNullOrWhiteSpace(Ssl.CertPath))
                        connectionFactory.Ssl.CertPath = Ssl.CertPath;
                    if (!string.IsNullOrWhiteSpace(Ssl.CertPassphrase))
                        connectionFactory.Ssl.CertPassphrase = Ssl.CertPassphrase;
                    if (Ssl.Version != null)
                        connectionFactory.Ssl.Version = Ssl.Version.Value;
                    if (Ssl.AcceptablePolicyErrors != null)
                        connectionFactory.Ssl.AcceptablePolicyErrors = Ssl.AcceptablePolicyErrors.Value;
                }

                return connectionFactory;
            }
        }
    }

    /// <summary>TLS 子配置,拆出来避免主结构在没启用 TLS 的场景下堆一大堆无关字段。</summary>
    public class RabbitMqSslConfigure
    {
        /// <summary>是否启用 TLS;默认 false 保持向后兼容。</summary>
        public bool Enabled { get; set; }

        /// <summary>SNI / 证书主体名;为空时回退到连接层 HostName。</summary>
        public string ServerName { get; set; }

        /// <summary>客户端证书路径(.pfx / .p12),可选。</summary>
        public string CertPath { get; set; }

        /// <summary>客户端证书密码,可选。</summary>
        public string CertPassphrase { get; set; }

        /// <summary>显式指定 TLS 版本;null → 走 client 默认(通常 TLS 1.2/1.3)。</summary>
        public System.Security.Authentication.SslProtocols? Version { get; set; }

        /// <summary>
        /// 允许的证书校验瑕疵,例如自签场景需要 <c>RemoteCertificateChainErrors</c>。
        /// null → 严格模式(默认;生产建议保持严格并预置正确的信任链)。
        /// </summary>
        public SslPolicyErrors? AcceptablePolicyErrors { get; set; }
    }
}
