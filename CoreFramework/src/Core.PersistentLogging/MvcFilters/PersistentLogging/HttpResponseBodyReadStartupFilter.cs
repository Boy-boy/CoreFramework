using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public class HttpResponseBodyReadStartupFilter : IStartupFilter
    {
        public static event Func<HttpResponse, string> HttpResponseEvent;

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, next1) =>
                {
                    var originalResponseBody = context.Response.Body;
                    try
                    {
                        //声明一个MemoryStream替换Response Body
                        using var swapStream = new MemoryStream();
                        context.Response.Body = swapStream;

                        HttpResponseEvent = null;

                        await next1(context);

                        HttpResponseEvent?.Invoke(context.Response);

                        //重置标识位
                        context.Response.Body.Seek(0, SeekOrigin.Begin);
                        //把替换后的Response Body复制倒原始的Response Body
                        await swapStream.CopyToAsync(originalResponseBody);
                    }
                    finally
                    {
                        //无论异常与否都要把原始的Body给切换回来
                        context.Response.Body = originalResponseBody;
                    }
                });
                next(app);
            };
        }
    }
}
