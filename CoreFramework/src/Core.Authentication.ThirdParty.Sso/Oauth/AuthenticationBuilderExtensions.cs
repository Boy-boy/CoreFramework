using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddThirdPartyCookie(this AuthenticationBuilder builder)
        {
            return builder.AddThirdPartyCookie(_ => { });
        }

        public static AuthenticationBuilder AddThirdPartyCookie(this AuthenticationBuilder builder,
            Action<ThirdPartyAuthenticationCookieOptions> configureOptions)
        {
            builder.Services.AddDataProtection();
            return builder.AddScheme<ThirdPartyAuthenticationCookieOptions, ThirdPartyAuthenticationCookieHandler>(CookieDefault.AuthenticationScheme, CookieDefault.DisplayName, configureOptions);
        }
    }
}
