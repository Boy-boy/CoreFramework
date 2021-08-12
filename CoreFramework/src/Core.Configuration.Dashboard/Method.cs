using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Core.Configuration.Dashboard
{
    public class Method<TService> : IMethod
    {
        private MethodInfo _methodInvoke;

        private string _routeTemplate;

        private List<object> _methodMetadata;

        private string _httpMetadata;

        public Method(
            string methodName,
            Type methodParameter = null)
        {
            MethodParameter = methodParameter;
            MethodName = methodName;
        }

        public string MethodName { get; set; }

        public Type MethodParameter { get; set; }

        public string HttpMetadata => _httpMetadata ??= GetHttpMetadata();

        public string RouteTemplate => _routeTemplate ??= GetRouteTemplate();

        public List<object> MethodMetadata => _methodMetadata ??= GetMethodMetadata();

        public MethodInfo MethodInvoke => _methodInvoke ??= GetMethod();


        private string GetRouteTemplate()
        {
            var handlerMethod = MethodInvoke;
            var controllerRoute = typeof(TService).Name;
            var actionRoute = MethodName;

            var classRouteMetadata = typeof(TService).GetCustomAttributes(inherit: true).LastOrDefault(attribute => attribute is RouteAttribute);
            var methodRouteMetadata = handlerMethod.GetCustomAttributes(inherit: true).LastOrDefault(attribute => attribute is RouteAttribute);
            if (classRouteMetadata != null)
            {
                var routeAttribute = classRouteMetadata as RouteAttribute;
                controllerRoute = routeAttribute?.Template;
            }
            if (methodRouteMetadata != null)
            {
                var actionRouteAttribute = methodRouteMetadata as RouteAttribute;
                actionRoute = actionRouteAttribute?.Template;
            }
            return "/" + controllerRoute + "/" + actionRoute;
        }

        private string GetHttpMetadata()
        {
            var httpMetadata = "POST";
            var handlerMethod = MethodInvoke;
            var methodRouteMetadata = handlerMethod.GetCustomAttributes(inherit: true).LastOrDefault(attribute => attribute is HttpMethodAttribute);
            if (methodRouteMetadata != null)
            {
                var actionRouteAttribute = methodRouteMetadata as HttpMethodAttribute;
                httpMetadata = actionRouteAttribute?.HttpMethods.FirstOrDefault();
            }
            return httpMetadata;
        }

        private List<object> GetMethodMetadata()
        {
            var handlerMethod = MethodInvoke;
            var metadata = new List<object>();
            metadata.AddRange(typeof(TService).GetCustomAttributes(inherit: true));
            metadata.AddRange(handlerMethod.GetCustomAttributes(inherit: true));
            return metadata;
        }

        private MethodInfo GetMethod()
        {
            var types = new[] { typeof(HttpContext), typeof(CancellationToken) };
            if (MethodParameter != null)
            {
                types = new[] { MethodParameter, typeof(HttpContext), typeof(CancellationToken) };
            }
            var currentType = typeof(TService);
            var matchingMethod = currentType.GetMethod(
                MethodName,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: types,
                modifiers: null);

            if (matchingMethod == null)
            {
                throw new InvalidOperationException($"Could not find '{MethodName}' on {typeof(TService)}.");
            }

            return matchingMethod;
        }
    }
}
