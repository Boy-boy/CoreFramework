using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Core.Authentication.ThirdParty.Sso.Oauth.Helper;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public class PrePipelineExecutingLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PrePipelineExecutingLogMiddleware> _logger;

        public PrePipelineExecutingLogMiddleware(RequestDelegate next,
            ILogger<PrePipelineExecutingLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("******开始执行第三方单点登录*****");
            var currentStartUri = context.Request.Scheme + Uri.SchemeDelimiter + context.Request.Host + context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            _logger.LogInformation($"******执行前请求Uri: {currentStartUri}*****");
            _logger.LogInformation($"******执行前请求X-Forwarded-Host: {context.GetRealClientHost()}*****");
            _logger.LogInformation($"******执行前请求X-Forwarded-Proto: {context.GetRealClientScheme()}*****");

            await _next(context);

            _logger.LogInformation("******结束执行第三方单点登录*****");
            var currentEndUri = context.Request.Scheme + Uri.SchemeDelimiter + context.Request.Host + context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            _logger.LogInformation($"******执行后请求Uri: {currentEndUri}*****");
        }
    }
}
