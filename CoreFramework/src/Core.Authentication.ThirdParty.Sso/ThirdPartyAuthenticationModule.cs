using Core.Authentication.ThirdParty.Sso.Oauth;
using Core.Modularity;
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
            context.Services.AddThirdPartyAuthentication(_configuration);
        }

        public override void PreConfigure(ApplicationBuilderContext context)
        {
            var app = context.ApplicationBuilder;
            app.UseThirdPartyAuthentication();
        }
    }
}
