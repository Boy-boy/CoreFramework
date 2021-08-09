using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Core.Configuration.Dashboard
{
    public class ServiceMethodProviderContext<TService> where TService : class
    {
        public ServiceMethodProviderContext()
        {
            Methods = new List<MethodModel>();
        }
        internal List<MethodModel> Methods { get; }

        public void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, IList<object> metadata, ServerMethod<TService, TRequest, TResponse> invoker)
        {
            var callHandler = new ServerCallHandler<TService, TRequest, TResponse>(invoker);
            var methodModel = new MethodModel(RoutePatternFactory.Parse(method.FullName), metadata, callHandler.HandleCallAsync);
            Methods.Add(methodModel);
        }
    }
}
