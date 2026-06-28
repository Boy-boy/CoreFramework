using Core.Json.SystemTextJson;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Configuration.Dashboard
{
    public class ServerCallHandler<TService>
    {
        private readonly Method<TService> _method;

        private static readonly Lazy<ObjectFactory> ObjectFactory = new Lazy<ObjectFactory>(() => ActivatorUtilities.CreateFactory(typeof(TService), Type.EmptyTypes));

        public ServerCallHandler(Method<TService> method)
        {
            _method = method;
        }

        public async Task HandleCallAsync(HttpContext httpContext)
        {
            var httpMethod = httpContext.Request.Method;
            switch (httpMethod)
            {
                case "GET" when httpMethod == _method.HttpMetadata:
                    {
                        var service = CreateService(httpContext.RequestServices);
                        if (_method.MethodParameter != null)
                        {
                            var request = httpContext.Request.Query.FirstOrDefault();
                            _method.MethodInvoke.Invoke(service, new object[]
                            {
                                    request.Value.ToString(),
                                    httpContext,
                                    httpContext.RequestAborted
                            });
                        }
                        else
                        {
                            _method.MethodInvoke.Invoke(service, new object[]
                            {
                                    httpContext,
                                    httpContext.RequestAborted
                            });
                        }
                        break;
                    }
                case "POST" when httpMethod == _method.HttpMetadata:
                    {
                        var service = CreateService(httpContext.RequestServices);
                        if (_method.MethodParameter != null)
                        {
                            httpContext.Request.EnableBuffering();
                            httpContext.Request.Body.Position = 0;
                            var streamReader = new StreamReader(httpContext.Request.Body);
                            var body = streamReader.ReadToEndAsync().GetAwaiter().GetResult();
                            var request = body.ToObject(_method.MethodParameter);
                            _method.MethodInvoke.Invoke(service, new[]
                             {
                                   request,
                                   httpContext,
                                   httpContext.RequestAborted
                                 });
                            httpContext.Request.Body.Position = 0;
                        }
                        else
                        {
                            _method.MethodInvoke.Invoke(service, new object[]
                             {
                                     httpContext,
                                     httpContext.RequestAborted
                             });
                        }
                        break;
                    }
                case "DELETE" when httpMethod == _method.HttpMetadata:
                    {
                        var service = CreateService(httpContext.RequestServices);
                        if (_method.MethodParameter != null)
                        {
                            var request = httpContext.Request.Query.FirstOrDefault();
                            _method.MethodInvoke.Invoke(service, new object[]
                            {
                            request.Value.ToString(),
                            httpContext,
                            httpContext.RequestAborted
                            });
                        }
                        else
                        {
                            _method.MethodInvoke.Invoke(service, new object[]
                            {
                            httpContext,
                            httpContext.RequestAborted
                            });
                        }
                        break;
                    }
                default:
                    throw new Exception("Db Config Data Api暂且只支持GET,POST,DELETE请求");
            }
            await Task.CompletedTask;
        }

        public TService CreateService(IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetService<TService>();
            if (service != null) return service;
            service = (TService)ObjectFactory.Value(serviceProvider, Array.Empty<object>());
            return service;

        }
    }
}
