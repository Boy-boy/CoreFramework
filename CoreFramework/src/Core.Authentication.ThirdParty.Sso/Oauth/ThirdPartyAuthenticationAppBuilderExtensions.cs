using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public static class ThirdPartyAuthenticationAppBuilderExtensions
    {
        public static IApplicationBuilder UseThirdPartyAuthentication(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            app.Map(configuration.GetValue<string>("ThirdPartyAuthentication:PathBase")!,
                app1 =>
                {
                    app1.UseMiddleware<ThirdPartyAuthenticationSignOutMiddleware>();
                    app1.UseMiddleware<ThirdPartyAuthenticationMiddleware>();
                });
            return app;
        }
    }
}
