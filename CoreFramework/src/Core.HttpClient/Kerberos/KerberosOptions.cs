namespace Core.HttpClient.Kerberos
{
    /// <summary>
    /// Kerberos 认证配置选项，用于配置客户端访问 Kerberized 服务所需的参数。
    /// 支持从 Base64 编码的 keytab 或文件路径加载密钥，优先使用 Base64。
    /// </summary>
    public class KerberosOptions
    {
        /// <summary>
        /// 配置节名称，用于在 appsettings.json 中绑定配置。
        /// 示例：{"Kerberos": { ... }}
        /// </summary>
        public const string SectionName = "Kerberos";

        /// <summary>
        /// Kerberos 主体名称（Principal），格式通常为：username@REALM 或 service/host@REALM。
        /// 必须与 keytab 文件中包含的主体一致。
        /// 示例：httpclient@EXAMPLE.COM
        /// </summary>
        public string Principal { get; set; } = string.Empty;

        /// <summary>
        /// keytab 文件内容的 Base64 编码字符串。
        /// 推荐用于生产环境，可通过 Kubernetes Secret、环境变量等方式注入。
        /// 如果设置，将优先于 KeytabPath 使用。
        /// </summary>
        public string KeytabBase64 { get; set; } = string.Empty;

        /// <summary>
        /// keytab 文件在容器或系统中的路径。
        /// 默认为 /etc/krb5.keytab，适用于通过 Kubernetes Secret 挂载的场景。
        /// 如果 KeytabBase64 未设置，则尝试从此路径读取文件。
        /// 注意：确保应用对该路径有读取权限。
        /// </summary>
        public string KeytabPath { get; set; } = "/etc/krb5.keytab";

        /// <summary>
        /// Kerberos 领域名称（Realm），通常为大写的域名。
        /// 必须与 KDC 和 Principal 的领域部分匹配。
        /// 示例：EXAMPLE.COM
        /// </summary>
        public string Realm { get; set; } = string.Empty;

        /// <summary>
        /// KDC（Key Distribution Center）服务器地址，格式为 host:port。
        /// 必须可从 Pod 或主机网络访问。
        /// 示例：kdc.example.com:88
        /// </summary>
        public string Kdc { get; set; } = string.Empty;

        /// <summary>
        /// 默认服务主体名称（Service Principal Name, SPN），用于生成服务票据。
        /// 格式通常为：服务类/主机名@REALM，如 HTTP/webservice.example.com。
        /// 如果调用 GetNegotiateTokenAsync 时未指定 SPN，则使用此值。
        /// </summary>
        public string DefaultServiceSpn { get; set; } = string.Empty;

        /// <summary>
        /// 指定请求的 Kerberos 票据（TGT）的生命周期（有效期）。
        /// 此值会在 AS-REQ 请求中作为建议发送给 KDC，但最终有效期由 KDC 策略决定。
        /// 
        /// 示例：
        /// - 如果 KDC 配置最大为 8 小时，则即使设置为 24 小时，实际票据仍为 8 小时。
        /// - 建议设置为与 KDC 策略匹配的值，避免误解。
        /// 
        /// 注意：
        /// - 此值不影响本地缓存时间，仅用于认证请求。
        /// - 单位： TimeSpan，如 TimeSpan.FromHours(8)
        /// 
        /// 默认值：8 小时（仅为建议值，实际以 KDC 为准）
        /// </summary>
        public TimeSpan TicketLifetime { get; set; } = TimeSpan.FromHours(8);

        /// <summary>
        /// 指定 Kerberos 凭据（如 TGT）在过期前多久开始尝试自动续订。
        /// 设置合理的续订窗口可确保服务在票据到期前平滑完成更新，避免认证中断。
        /// 
        /// 例如：若 Kerberos TGT 有效期为 7 天，设置 RenewLifetime 为 7 天时，
        /// 系统将在首次获取票据后立即标记为“需续订”，因此建议设置为略小于票据总有效期（如 6.5 天）。
        /// 
        /// 注意：该值不应大于 Kerberos 票据的 renewable lifetime（可续订总时长），
        /// 否则续订请求将被 KDC 拒绝。
        /// </summary>
        public TimeSpan RenewLifetime { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Kerberos 票据（Ticket）在内存缓存中的保留时间。
        /// 在此期间内，相同服务的请求将复用缓存的票据，避免重复与 KDC 通信，显著提升性能。
        /// 
        /// 建议设置为略小于票据实际有效期（例如：票据有效期为 8 小时，可设为 7 小时或 7.5 小时），
        /// 以预留时间进行异步续订，防止因票据过期导致请求失败。
        /// 
        /// 缓存时间过短会增加与 KDC 的通信频率；过长则可能导致使用已过期票据的风险（受时钟偏移影响）。
        /// </summary>
        public TimeSpan TicketCacheDuration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// 允许的客户端与 KDC 服务器之间的时间偏差容忍值。
        /// Kerberos 协议对时间同步要求严格（通常要求偏差不超过 5 分钟），该值用于调整本地时间校验窗口。
        /// 
        /// 例如：当本地时间与 KDC 时间相差不超过此阈值时，票据仍被视为有效。
        /// 建议与企业域控的时钟同步策略（如 NTP 配置）保持一致，通常设置为 5 分钟。
        /// 
        /// 注意：若 ClockSkew 设置过大，可能降低安全性（增加重放攻击风险）；过小则可能导致频繁的 401 认证失败。
        /// </summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    }
}
