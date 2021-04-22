using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Uow
{
    public class CoreUowModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddUnitOfWork();
        }
    }
}
