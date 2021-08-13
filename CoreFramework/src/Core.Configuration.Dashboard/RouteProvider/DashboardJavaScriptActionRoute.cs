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
    [Route("config/dashboard/js")]
    public class DashboardJavaScriptActionRoute
    {
        [HttpGet]
        [Route("chunk-1939524c.329bb04a.js")]
        public async Task Get1Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-1939524c.329bb04a.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-388d9eec.d466b2f8.js")]
        public async Task Get2Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-388d9eec.d466b2f8.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-3b55dc7e.dc8c010e.js")]
        public async Task Get3Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-3b55dc7e.dc8c010e.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-3b7d2a35.d37341dd.js")]
        public async Task Get4Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-3b7d2a35.d37341dd.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-6673d798.f1dc7ffc.js")]
        public async Task Get5Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-6673d798.f1dc7ffc.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-7592249c.311b35c7.js")]
        public async Task Get6Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-7592249c.311b35c7.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-7af68eda.f77c397f.js")]
        public async Task Get7Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-7af68eda.f77c397f.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-7c518a68.90291691.js")]
        public async Task Get8Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-7c518a68.90291691.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-a5f2b902.b6ea9744.js")]
        public async Task Get9Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-a5f2b902.b6ea9744.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-c1fb19f0.dac6ed4e.js")]
        public async Task Get10Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-c1fb19f0.dac6ed4e.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-vendors.2317c5e0.js")]
        public async Task Get11Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".chunk-vendors.2317c5e0.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("index.db0cfe81.js")]
        public async Task Get12Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.js" + ".index.db0cfe81.js");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        internal static List<Method<DashboardJavaScriptActionRoute>> GetMethods()
        {
            var methods = new List<Method<DashboardJavaScriptActionRoute>>
            {
                new Method<DashboardJavaScriptActionRoute>( "Get1Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get2Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get3Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get4Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get5Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get6Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get7Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get8Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get9Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get10Async"),
                new Method<DashboardJavaScriptActionRoute>( "Get11Async"),
                new Method<DashboardJavaScriptActionRoute>("Get12Async")
            };
            return methods;
        }
    }
}
