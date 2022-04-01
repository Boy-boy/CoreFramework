using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Amazon.S3
{
    public class AmazonS3Module : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public AmazonS3Module(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddAmazonS3(Configuration.GetSection("Amazon:S3"));
        }
    }
}
