using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    /// <summary>
    /// 自定义转发标头中间件
    /// </summary>
    public class CustomForwardedHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ThirdPartyAuthenticationOptions _options;

        public CustomForwardedHeadersMiddleware(RequestDelegate next,
            IOptions<ThirdPartyAuthenticationOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            var realClientUriScheme = _options.RealClientUriScheme;
            context.Request.Scheme = string.IsNullOrEmpty(realClientUriScheme)
                ? context.Request.Scheme
                : realClientUriScheme;

            var realClientUriHost = _options.RealClientUriHost;
            context.Request.Host = string.IsNullOrEmpty(realClientUriHost)
                ? context.Request.Host
                : context.Request.Host.Port.HasValue
                    ? new HostString(realClientUriHost, context.Request.Host.Port.Value)
                    : new HostString(realClientUriHost);

            var realClientUriPort = _options.RealClientUriPort;
            context.Request.Host = !realClientUriPort.HasValue
                ? context.Request.Host
                : new HostString(context.Request.Host.Host, realClientUriPort.Value);

            var realClientUriPathBase = _options.RealClientUriPathBase;
            context.Request.PathBase = string.IsNullOrEmpty(realClientUriPathBase)
                ? context.Request.PathBase
                : realClientUriPathBase;

            await _next(context);
        }
    }

}
