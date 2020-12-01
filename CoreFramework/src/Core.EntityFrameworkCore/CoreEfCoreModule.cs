using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EntityFrameworkCore
{
    public class CoreEfCoreModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddEntityFrameworkRepository();
        }
    }
}
