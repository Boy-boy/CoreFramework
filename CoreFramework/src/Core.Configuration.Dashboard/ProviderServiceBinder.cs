using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Core.Configuration.Dashboard
{
    public class ProviderServiceBinder<TService> where TService : class
    {
        private readonly ServiceMethodProviderContext<TService> _context;

        public ProviderServiceBinder(ServiceMethodProviderContext<TService> context)
        {
            _context = context;
        }

        public void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method)
        {
            var (invoker, metadata) = CreateModelCore<ServerMethod<TService, TRequest, TResponse>>(
                method.Name, method.HttpMetadata,
                new[] { typeof(TRequest),typeof(CancellationToken) });

            _context.AddMethod(method, metadata, invoker);
        }

        private (TDelegate invoker, List<object> metadata) CreateModelCore<TDelegate>(string methodName, string httpMetadata, Type[] methodParameters) where TDelegate : Delegate
        {
            var handlerMethod = GetMethod(methodName, methodParameters);

            if (handlerMethod == null)
            {
                throw new InvalidOperationException($"Could not find '{methodName}' on {typeof(TService)}.");
            }

            var invoker = (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), handlerMethod);

            var metadata = new List<object>();
            metadata.AddRange(typeof(TService).GetCustomAttributes(inherit: true));
            metadata.AddRange(handlerMethod.GetCustomAttributes(inherit: true));

            metadata.Add(new HttpMethodMetadata(new[] { httpMetadata ?? "POST" }, true));
            return (invoker, metadata);
        }

        private MethodInfo GetMethod(string methodName, Type[] methodParameters)
        {
            var currentType = typeof(TService);
            var matchingMethod = currentType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: methodParameters,
                modifiers: null);

            return matchingMethod == null ? null : matchingMethod;
        }
    }
}
