using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    /// <summary>
    /// 第三方认证平台登出中间件
    /// </summary>
    internal class ThirdPartyAuthenticationSignOutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ThirdPartyAuthenticationOptions _options;

        public ThirdPartyAuthenticationSignOutMiddleware(RequestDelegate next,
            IOptions<ThirdPartyAuthenticationOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            var schemeName = _options.DefaultScheme;
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

            var signOutPath = _options.SignOutPath;
            if (context.Request.Path.HasValue && context.Request.Path == signOutPath)
            {
                await context.SignOutAsync(schemeName);
                return;
            }
            await _next(context);
        }
    }
}
