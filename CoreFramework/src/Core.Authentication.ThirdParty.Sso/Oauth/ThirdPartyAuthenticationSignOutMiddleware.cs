using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    /// <summary>
    /// 第三方认证平台登出中间件
    /// </summary>
    internal class ThirdPartyAuthenticationSignOutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public ThirdPartyAuthenticationSignOutMiddleware(RequestDelegate next,
            IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var schemeName = _configuration.GetValue<string>("ThirdPartyAuthentication:DefaultScheme");
            if (string.IsNullOrWhiteSpace(schemeName))
            {
                await _next(context);
                return;
            }

            context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
            {
                OriginalPath = context.Request.Path,
                OriginalPathBase = context.Request.PathBase
            });

            var signOutPath = _configuration.GetValue<string>("ThirdPartyAuthentication:SignOutPath");
            if (context.Request.Path.HasValue && context.Request.Path == signOutPath)
            {
                await context.SignOutAsync(schemeName);
                return;
            }
            await _next(context);
        }
    }
}
