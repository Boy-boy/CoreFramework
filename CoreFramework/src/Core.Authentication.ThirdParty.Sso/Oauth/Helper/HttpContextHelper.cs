using Microsoft.AspNetCore.Http;
using System.Net;

namespace Core.Authentication.ThirdParty.Sso.Oauth.Helper
{
    public static class HttpContextHelper
    {
        /// <summary>
        /// 获取真实的客户端ip
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetRealClientIp(this HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            if (!context.Request.Headers.ContainsKey("X-Forwarded-For"))
                return clientIp;
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"];

            if (string.IsNullOrEmpty(xForwardedFor))
                return clientIp;

            // 多个IP地址时，X-Forwarded-For的格式为：clientIP1, proxyIP1, proxyIP2, ...
            var ips = xForwardedFor.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // 获取第一个非空白和回送地址的IP
            foreach (var ip in ips)
            {
                if (!IPAddress.TryParse(ip.Trim(), out var parsedIp) || IPAddress.IsLoopback(parsedIp))
                {
                    continue;
                }

                clientIp = ip.Trim();
                break;
            }

            return clientIp;
        }

        /// <summary>
        /// 获取真实的客户端host
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetRealClientHost(this HttpContext context)
        {
            var forwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
            var clientHost = string.IsNullOrEmpty(forwardedHost) ? context.Request.Host.Host : forwardedHost;
            return clientHost;
        }

        /// <summary>
        /// 获取真实的客户端scheme
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetRealClientScheme(this HttpContext context)
        {
            var forwardedScheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
            var clientScheme = string.IsNullOrEmpty(forwardedScheme) ? context.Request.Scheme : forwardedScheme;
            return clientScheme;
        }
    }
}
