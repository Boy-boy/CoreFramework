using Microsoft.AspNetCore.Authentication;

namespace ThirdPartySso.WebApi.SsoProviders.YXST
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddYXSTOauth(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            IConfiguration configuration)
        {
            builder.Services.Configure<YXSTOauthOptions>(authenticationScheme, configuration);
            return builder.AddYXSTOauth<YXSTOauthOptions, YxstOauthHandler<YXSTOauthOptions>>(authenticationScheme, _ => { });
        }
        public static AuthenticationBuilder AddYXSTOauth(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<YXSTOauthOptions> configureOptions)
        {
            return builder.AddYXSTOauth<YXSTOauthOptions, YxstOauthHandler<YXSTOauthOptions>>(authenticationScheme, configureOptions);
        }

        public static AuthenticationBuilder AddYXSTOauth<TOptions, THandler>(this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<TOptions> configureOptions)
            where TOptions : YXSTOauthOptions, new()
            where THandler : YxstOauthHandler<TOptions>
        {
            return builder.AddOAuth<TOptions, THandler>(authenticationScheme, "远信数通 Oauth", configureOptions);
        }
    }
}
