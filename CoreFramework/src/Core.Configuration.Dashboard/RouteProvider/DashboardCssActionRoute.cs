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
    [Route("config/dashboard/css")]
    public class DashboardCssActionRoute
    {
        [HttpGet]
        [Route("chunk-1939524c.e374acd7.css")]
        public async Task Get1Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-1939524c.e374acd7.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-388d9eec.191522a0.css")]
        public async Task Get2Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-388d9eec.191522a0.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-3b55dc7e.3fa8276e.css")]
        public async Task Get3Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-3b55dc7e.3fa8276e.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-3b7d2a35.47b7ed2e.css")]
        public async Task Get4Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-3b7d2a35.47b7ed2e.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-6673d798.fa808e46.css")]
        public async Task Get5Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-6673d798.fa808e46.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-7592249c.200fdbc3.css")]
        public async Task Get6Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-7592249c.200fdbc3.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-7af68eda.af8c3925.css")]
        public async Task Get7Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-7af68eda.af8c3925.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-7c518a68.587716c5.css")]
        public async Task Get8Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-7c518a68.587716c5.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-a5f2b902.11c50a80.css")]
        public async Task Get9Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-a5f2b902.11c50a80.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-c1fb19f0.8e29003d.css")]
        public async Task Get10Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-c1fb19f0.8e29003d.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("chunk-vendors.230de3f5.css")]
        public async Task Get11Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".chunk-vendors.230de3f5.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        [HttpGet]
        [Route("index.cdb09893.css")]
        public async Task Get12Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.css" + ".index.cdb09893.css");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), cancellationToken);
        }

        internal static List<Method<DashboardCssActionRoute>> GetMethods()
        {
            var methods = new List<Method<DashboardCssActionRoute>>
            {
                new Method<DashboardCssActionRoute>( "Get1Async"),
                new Method<DashboardCssActionRoute>( "Get2Async"),
                new Method<DashboardCssActionRoute>( "Get3Async"),
                new Method<DashboardCssActionRoute>( "Get4Async"),
                new Method<DashboardCssActionRoute>( "Get5Async"),
                new Method<DashboardCssActionRoute>( "Get6Async"),
                new Method<DashboardCssActionRoute>( "Get7Async"),
                new Method<DashboardCssActionRoute>( "Get8Async"),
                new Method<DashboardCssActionRoute>( "Get9Async"),
                new Method<DashboardCssActionRoute>( "Get10Async"),
                new Method<DashboardCssActionRoute>( "Get11Async"),
                new Method<DashboardCssActionRoute>( "Get12Async")
            };
            return methods;
        }
    }
}
