using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public static class ThirdPartyAuthenticationAppBuilderExtensions
    {
        public static IApplicationBuilder UseThirdPartyAuthentication(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            var thirdPartyAuthenticationOptions = app.ApplicationServices.GetRequiredService<IOptions<ThirdPartyAuthenticationOptions>>().Value;
            app.Map(thirdPartyAuthenticationOptions.PathBase,
                app1 =>
                {
                    //TODO：中间件顺序不可随意改变
                    app1.UseMiddleware<GlobalExceptionMiddleware>();
                    app1.UseMiddleware<PrePipelineExecutingLogMiddleware>();
                    app1.UseForwardedHeaders(new ForwardedHeadersOptions()
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto,
                        ForwardLimit = null
                    });
                    app1.UseMiddleware<CustomForwardedHeadersMiddleware>();
                    app1.UseMiddleware<ThirdPartyAuthenticationSignOutMiddleware>();
                    app1.UseMiddleware<ThirdPartyAuthenticationMiddleware>();
                });
            return app;
        }
    }
}
