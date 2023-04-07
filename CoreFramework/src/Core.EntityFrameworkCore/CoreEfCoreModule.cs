using Core.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Core.Modularity.Attribute;
using Core.Uow;

namespace Core.EntityFrameworkCore
{
    [DependsOn(typeof(CoreUnitOfWorkModule))]
    public class CoreEfCoreModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            var dbContextTypes = context.Items.Values
                .Select(value => value as Type)
                .Where(value => typeof(DbContext).IsAssignableFrom(value))
                .Distinct()
                .ToList();
            dbContextTypes.ForEach(dbContextType =>
            {
                context.Services.AddRepositories(dbContextType);
            });
        }
    }
}
