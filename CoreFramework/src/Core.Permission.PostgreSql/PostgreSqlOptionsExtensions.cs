using System;
using Core.Permission.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Permission.Storage
{
    public class PostgreSqlOptionsExtensions : IPermissionOptionsExtensions
    {
        private readonly Action<PermissionPostgreSqlOptions> _actionOptions;

        public PostgreSqlOptionsExtensions(Action<PermissionPostgreSqlOptions> actionOptions)
        {
            _actionOptions = actionOptions ?? throw new ArgumentNullException(nameof(actionOptions));
        }
        public void AddServices(IServiceCollection services)
        {
            services.Configure(_actionOptions);
            services.TryAddSingleton(typeof(IPermissionGrantsStorage), typeof(PostgreSqlPermissionGrantsStorage));
        }
    }
}
