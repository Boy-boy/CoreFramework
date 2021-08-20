using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System;
using System.Reflection;

namespace Core.Configuration.Dashboard
{
    public class DashboardStaticFileStartupFilter : IStartupFilter
    {
        private const string EmbeddedFileNamespace = "Core.Configuration.Dashboard.wwwroot.dist";
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return (app =>
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new EmbeddedFileProvider(typeof(DashboardStaticFileStartupFilter).GetTypeInfo().Assembly, EmbeddedFileNamespace),
                    RequestPath = "/config/dashboard"
                });
                next(app);
            });
        }
    }
}
