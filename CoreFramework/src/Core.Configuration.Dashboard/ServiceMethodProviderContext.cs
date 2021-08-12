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

        public void AddMethod(Method<TService> method)
        {
            var callHandler = new ServerCallHandler<TService>(method);
            var methodModel = new MethodModel(RoutePatternFactory.Parse(method.RouteTemplate), method.MethodMetadata, callHandler.HandleCallAsync);
            Methods.Add(methodModel);
        }
    }
}
