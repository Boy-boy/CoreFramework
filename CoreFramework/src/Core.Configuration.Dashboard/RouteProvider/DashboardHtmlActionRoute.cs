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
    [Route("config/dashboard")]
    public class DashboardHtmlActionRoute
    {

        [HttpGet]
        [Route("index")]
        public async Task GetIndexAsync(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist" + ".index.html");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8, cancellationToken);
        }

        [HttpGet]
        [Route("favicon.ico")]
        public async Task Get1Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist" + ".favicon.ico");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8, cancellationToken);
        }

        internal static List<Method<DashboardHtmlActionRoute>> GetMethods()
        {
            var methods = new List<Method<DashboardHtmlActionRoute>>
            {
                new Method<DashboardHtmlActionRoute>( "GetIndexAsync"),
                new Method<DashboardHtmlActionRoute>( "Get1Async"),
            };
            return methods;
        }
    }
}
