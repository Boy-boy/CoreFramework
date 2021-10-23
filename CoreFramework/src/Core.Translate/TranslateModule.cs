using Core.Modularity;
using Core.Translate.BaiDu;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Translate
{
    public class TranslateModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public TranslateModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            var services = context.Services;

            services.Configure<BaiDuTranslateOptions>(Configuration.GetSection("Translate:BaiDu"));
            services.AddBaiDuTranslate();
        }
    }
}
