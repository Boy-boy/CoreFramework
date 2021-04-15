using System;
using System.Linq;
using Core.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EntityFrameworkCore
{
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
                context.Services.AddEfRepositories(dbContextType);
            });
            context.Services.AddUnitOfWork();
        }
    }
}
