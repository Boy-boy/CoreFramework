using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Pipeline
{
    public class PipelineModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddPipeline();
        }
    }
}
