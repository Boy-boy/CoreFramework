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
    [Route("img")]
    public class DashboardImgActionRoute
    {
        [HttpGet]
        [Route("404.764e3699.png")]
        public async Task Get1Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly
                .GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.img" + ".404.764e3699.png");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(),  cancellationToken);
        }

        [HttpGet]
        [Route("boxShadow.51818d84.png")]
        public async Task Get2Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly
                .GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.img" + ".boxShadow.51818d84.png");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(),  cancellationToken);
        }

        [HttpGet]
        [Route("login_bg.993b9c92.png")]
        public async Task Get3Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly
                .GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.img" + ".login_bg.993b9c92.png");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(),  cancellationToken);
        }

        [HttpGet]
        [Route("login_footer.0402bdb6.png")]
        public async Task Get4Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly
                .GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.img" +
                                           ".login_footer.0402bdb6.png");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(),  cancellationToken);
        }

        [HttpGet]
        [Route("logoTitleNew.201b37cb.png")]
        public async Task Get5Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly
                .GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.img" +
                                           ".logoTitleNew.201b37cb.png");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(),  cancellationToken);
        }

        [HttpGet]
        [Route("pieBg.c69b4de0.c69b4de0.png")]
        public async Task Get6Async(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly
                .GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.dist.img" +
                                           ".pieBg.c69b4de0.c69b4de0.png");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(),  cancellationToken);
        }

        internal static List<Method<DashboardImgActionRoute>> GetMethods()
        {
            var methods = new List<Method<DashboardImgActionRoute>>
            {
                new Method<DashboardImgActionRoute>("Get1Async"),
                new Method<DashboardImgActionRoute>("Get2Async"),
                new Method<DashboardImgActionRoute>("Get3Async"),
                new Method<DashboardImgActionRoute>("Get4Async"),
                new Method<DashboardImgActionRoute>("Get5Async"),
                new Method<DashboardImgActionRoute>("Get6Async")
            };
            return methods;
        }
    }
}
