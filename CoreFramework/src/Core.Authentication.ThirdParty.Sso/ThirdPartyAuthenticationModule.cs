using Core.Authentication.ThirdParty.Sso.Oauth;
using Core.Authentication.ThirdParty.Sso.Oauth.SignalR;
using Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Core.Authentication.ThirdParty.Sso
{
    public class CoreThirdPartyAuthenticationModule : CoreModuleBase
    {
        private readonly IConfiguration _configuration;

        public CoreThirdPartyAuthenticationModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void PostConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddThirdPartyAuthentication(_configuration.GetSection("ThirdPartyAuthentication"));
        }

        public override void PreConfigure(ApplicationBuilderContext context)
        {
            var app = context.ApplicationBuilder;
            app.UseThirdPartyAuthentication();
        }

        public override void PostConfigure(ApplicationBuilderContext context)
        {
            var app = context.ApplicationBuilder;
            if (!app.Properties.TryGetValue("__EndpointRouteBuilder", out var obj))
                return;
            var endpointRouteBuilder = (IEndpointRouteBuilder)obj;
            endpointRouteBuilder!.MapHub<SignOutNotificationHub>("/signOutHub");
        }
    }
}
