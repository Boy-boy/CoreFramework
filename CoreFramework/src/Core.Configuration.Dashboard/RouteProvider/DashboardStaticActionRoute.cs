using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.Dashboard
{
    [Route("config/dashboard/static")]
    public class DashboardStaticActionRoute
    {
        [HttpGet]
        [Route("config.js")]
        public async Task Get1Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.static" + ".config.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(),  cancellationToken);
        }

        [HttpGet]
        [Route("business.js")]
        public async Task Get2Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.static" + ".business.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(),  cancellationToken);
        }

        internal static List<Method<DashboardStaticActionRoute>> GetMethods()
        {
            var methods = new List<Method<DashboardStaticActionRoute>>
            {
                new Method<DashboardStaticActionRoute>( "Get1Async"),
                new Method<DashboardStaticActionRoute>( "Get2Async"),
            };
            return methods;
        }
    }
}
