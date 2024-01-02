using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    /// <summary>
    /// 第三方认证平台认证中间件
    /// </summary>
    internal class ThirdPartyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly ThirdPartyAuthenticationOptions _options;


        public ThirdPartyAuthenticationMiddleware(RequestDelegate next,
            IAuthenticationSchemeProvider schemes,
            IOptions<ThirdPartyAuthenticationOptions> options)
        {
            _next = next;
            _schemes = schemes;
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
            var scheme = await _schemes.GetSchemeAsync(schemeName);
            if (scheme != null)
            {
                var handlers = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                if (await handlers.GetHandlerAsync(context, scheme.Name) is IAuthenticationRequestHandler handler && await handler.HandleRequestAsync())
                {
                    return;
                }

                //第三方单点登录平台做认证
                await context.ChallengeAsync(schemeName);
                return;
            }
            await _next(context);
        }
    }
}
