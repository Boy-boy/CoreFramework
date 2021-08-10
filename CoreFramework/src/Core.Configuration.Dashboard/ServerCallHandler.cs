using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Core.Json.Newtonsoft;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Configuration.Dashboard
{
    public class ServerCallHandler<TService, TRequest, TResponse>
    {
        private readonly ServerMethod<TService, TRequest, TResponse> _invoker;

        private static readonly Lazy<ObjectFactory> ObjectFactory = new Lazy<ObjectFactory>(() => ActivatorUtilities.CreateFactory(typeof(TService), Type.EmptyTypes));


        public ServerCallHandler(ServerMethod<TService, TRequest, TResponse> invoker)
        {
            _invoker = invoker;
        }

        public async Task HandleCallAsync(HttpContext httpContext)
        {
            httpContext.Request.EnableBuffering();
            httpContext.Request.Body.Position = 0;
            var streamReader = new StreamReader(httpContext.Request.Body);
            var body = await streamReader.ReadToEndAsync();

            var service = CreateService(httpContext.RequestServices);
            var request = body.ToObject<TRequest>();
            var result = await _invoker.Invoke(service, request, httpContext.RequestAborted);

            await httpContext.Response.WriteAsync("", Encoding.UTF8, httpContext.RequestAborted);
            httpContext.Request.Body.Position = 0;
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
