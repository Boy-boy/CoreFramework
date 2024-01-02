using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                var errorMsg = e.InnerException != null ? e.InnerException.Message : e.Message;

                var data = new
                {
                    Success = false,
                    Code = 500,
                    Message = errorMsg,
                };
                context.Response.ContentType = "text/plain;charset=utf-8";
                await context.Response.WriteAsync(JsonSerializer.Serialize(data, new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                }));
                _logger.LogError($"集成第三方sso失败，错误原因：{errorMsg}");
            }
        }

    }
}
