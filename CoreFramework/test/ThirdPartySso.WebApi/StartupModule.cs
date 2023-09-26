using Core.Authentication.ThirdParty.Sso;
using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.AspNetCore.Authentication;
using ThirdPartySso.WebApi.SsoProviders.YXST;

namespace ThirdPartySso.WebApi
{
    [DependsOn(typeof(CoreThirdPartyAuthenticationModule))]
    public class StartupModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddControllers();
            context.Services.AddEndpointsApiExplorer();
            context.Services.AddSwaggerGen();

            new AuthenticationBuilder(context.Services)
                .AddYXSTOauth("YXSTOauth", Configuration.GetSection("ThirdPartyAuthentication:Schemes:YXSTOauth"));
            context.Services.Configure<YXSTOauthOptions>("YXSTOauth", options =>
            {
                options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
            });
        }

        public override void Configure(ApplicationBuilderContext context)
        {
            var app = context.ApplicationBuilder;

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
