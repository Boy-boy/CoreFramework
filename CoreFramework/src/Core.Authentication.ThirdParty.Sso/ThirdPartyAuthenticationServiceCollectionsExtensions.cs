using Core.Authentication.ThirdParty.Sso.Oauth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Authentication.ThirdParty.Sso
{
    public static class ThirdPartyAuthenticationServiceCollectionsExtensions
    {
        public static IServiceCollection AddThirdPartyAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddSignalR();
            services.AddAuthentication()
                .AddThirdPartyCookie();

            return services;
        }
    }
}
