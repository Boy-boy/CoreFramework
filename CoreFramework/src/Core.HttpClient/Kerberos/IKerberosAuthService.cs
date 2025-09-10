using Kerberos.NET.Entities;

namespace Core.HttpClient.Kerberos
{
    public interface IKerberosAuthService
    {
        /// <summary>
        /// 获取用于 HTTP Negotiate 认证的 Base64 令牌。
        /// </summary>
        /// <param name="servicePrincipalName">服务主体名称，如：http/api.example.com@DOMAIN.COM</param>
        /// <returns>Base64 编码的认证令牌</returns>
        Task<string> GetNegotiateTokenAsync(string servicePrincipalName);

        /// <summary>
        /// 获取原始 Kerberos 服务票据（AP-REQ 结构），用于自定义协议或高级场景。
        /// </summary>
        /// <param name="servicePrincipalName">服务主体名称</param>
        /// <returns>Kerberos AP-REQ 票据对象</returns>
        Task<KrbApReq> GetServiceTicketRawAsync(string servicePrincipalName);

        /// <summary>
        /// 检查 Kerberos 配置是否正确（如配置文件、时间偏差、凭据可用性等）。
        /// </summary>
        /// <returns>配置有效返回 true，否则返回 false</returns>
        Task<bool> ValidateKerberosConfigurationAsync();
    }
}
