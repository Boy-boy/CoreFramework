using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Core.Configuration.Dashboard
{
    public class DashboardStaticFileStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return (app =>
            {
                var path = Path.Combine(AppContext.BaseDirectory, "DbConfigStaticFile", "dist");
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(path),
                    RequestPath = "/config/dashboard"
                });
                next(app);
            });
        }
    }
}
