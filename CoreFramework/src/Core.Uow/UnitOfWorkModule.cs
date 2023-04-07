using Core.Modularity;

namespace Core.Uow
{
    public class CoreUnitOfWorkModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddUnitOfWork();
        }
    }
}
