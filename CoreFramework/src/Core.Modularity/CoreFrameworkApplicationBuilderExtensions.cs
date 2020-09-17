using Core.Modularity.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Modularity
{
    public static class CoreFrameworkApplicationBuilderExtensions
    {
        public static void ConfigureApplicationBuilder(this IApplicationBuilder app)
        {
            var application = app.ApplicationServices.GetRequiredService<ICoreApplication>();
            application.Configure(app);

            var requiredService = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            requiredService.ApplicationStopping.Register(() => application.Shutdown(app.ApplicationServices));
        }
    }
}
