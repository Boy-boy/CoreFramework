using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public static class ThirdPartyAuthenticationServiceCollectionsExtensions
    {
        public static IServiceCollection AddThirdPartyAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.Configure<ThirdPartyAuthenticationOptions>(configuration);
            services.AddHostedService<ThirdPartyAuthenticationBackgroundService>();

            services.AddAuthentication()
                .AddCookie(CookieDefault.AuthenticationScheme, CookieDefault.DisplayName, options =>
                {
                    options.Cookie.Name = CookieDefault.CookieName;
                });

            return services;
        }
    }
}
