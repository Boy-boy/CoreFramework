using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Core.Configuration.Dashboard
{
    public class DashboardStaticFileStartupFilter : IStartupFilter
    {
        private const string EmbeddedFileNamespace = "Core.Configuration.Dashboard.wwwroot.dist";

        private const string PathMatch = "/config/dashboard";

        private const string PathMatch1 = "/config/dashboard/home";
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return (app =>
            {
                app.Use(async (context, next1) =>
                {
                    var path = context.Request.Path;
                    if (path.Equals(PathMatch) || path.Equals(PathMatch1))
                    {
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "text/html;charset=utf-8";

                        await using var stream = GetType().Assembly.GetManifestResourceStream(EmbeddedFileNamespace + ".index.html");
                        if (stream == null) throw new InvalidOperationException();

                        using var sr = new StreamReader(stream);
                        var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
                        await context.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
                        return;
                    }
                    await next1();
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new EmbeddedFileProvider(typeof(DashboardStaticFileStartupFilter).GetTypeInfo().Assembly, EmbeddedFileNamespace),
                    RequestPath = PathMatch
                });
                next(app);
            });
        }
    }
}
