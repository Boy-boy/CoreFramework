using System;
using Microsoft.AspNetCore.Builder;

namespace Core.Uow
{
    public static class UnitOfWorkAppBuilderExtensions
    {
        public static IApplicationBuilder UseUnitOfWork(
            this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<UnitOfWorkMiddleware>();
        }
    }
}
