namespace Core.HttpClient.Kerberos
{
    public class KerberosAuthHandler : DelegatingHandler
    {
        private readonly IKerberosAuthService _kerberosAuthService;

        public KerberosAuthHandler(IKerberosAuthService kerberosAuthService)
        {
            _kerberosAuthService = kerberosAuthService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 从请求属性或配置中获取服务Principal名称
            var servicePrincipalName = GetServicePrincipalName(request);

            // 获取 Negotiate token
            var negotiateToken = await _kerberosAuthService.GetNegotiateTokenAsync(servicePrincipalName);

            // 添加认证头
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Negotiate", negotiateToken);

            return await base.SendAsync(request, cancellationToken);
        }

        private string GetServicePrincipalName(HttpRequestMessage request)
        {
            // 可以从多个地方获取服务Principal名称：

            // 1. 从请求属性中获取
            if (request.Options.TryGetValue(new HttpRequestOptionsKey<string>("ServicePrincipalName"), out var spn))
            {
                return spn;
            }

            // 2. 从请求头中获取
            if (request.Headers.TryGetValues("X-Kerberos-Service", out var values))
            {
                return values.FirstOrDefault();
            }

            // 3. 返回默认服务Principal（需要从配置获取）
            return null;
        }
    }
}
