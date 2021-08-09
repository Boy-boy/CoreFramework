using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;

namespace Core.Configuration.Dashboard
{
    public class ServiceRouteBuilder<TService> where TService : class
    {
        private readonly IEnumerable<IServiceMethodProvider<TService>> _serviceMethodProviders;

        public ServiceRouteBuilder(IEnumerable<IServiceMethodProvider<TService>> serviceMethodProviders)
        {
            _serviceMethodProviders = serviceMethodProviders;
        }

        internal List<IEndpointConventionBuilder> Build(
            IEndpointRouteBuilder endpointRouteBuilder)
        {
            var context = new ServiceMethodProviderContext<TService>();
            foreach (var serviceMethodProvider in _serviceMethodProviders)
                serviceMethodProvider.OnServiceMethodDiscovery(context);

            var conventionBuilderList = new List<IEndpointConventionBuilder>();
            if (context.Methods.Count <= 0) return conventionBuilderList;

            foreach (var method1 in context.Methods)
            {
                var method = method1;
                var conventionBuilder = endpointRouteBuilder.Map(method.Pattern, method.RequestDelegate);
                conventionBuilder.Add(ep =>
                {
                    ep.DisplayName = "dbConfig - " + method.Pattern.RawText;
                    foreach (var obj in method.Metadata)
                        ep.Metadata.Add(obj);
                });
                conventionBuilderList.Add(conventionBuilder);
            }
            return conventionBuilderList;
        }
    }
}
