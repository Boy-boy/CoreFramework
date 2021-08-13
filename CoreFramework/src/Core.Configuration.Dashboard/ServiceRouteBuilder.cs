using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Core.Configuration.Dashboard
{
    public class ServiceRouteBuilder
    {
        private readonly DashboardOptions _options;
        private readonly IEnumerable<IDashboardRouteProvider> _dashboardRouteProviders;

        public ServiceRouteBuilder(
            IOptions<DashboardOptions> options,
            IEnumerable<IDashboardRouteProvider> dashboardRouteProviders)
        {
            _options = options.Value;
            _dashboardRouteProviders = dashboardRouteProviders;
        }

        internal List<IEndpointConventionBuilder> Build(
            IEndpointRouteBuilder endpointRouteBuilder)
        {
            var conventionBuilderList = new List<IEndpointConventionBuilder>();
            foreach (var dashboardRouteProvider in _dashboardRouteProviders)
            {
                var context = dashboardRouteProvider.OnServiceMethodDiscovery();
                conventionBuilderList.AddRange(Build(endpointRouteBuilder, context.Methods));
            }
            return conventionBuilderList;
        }

        private List<IEndpointConventionBuilder> Build(
            IEndpointRouteBuilder endpointRouteBuilder,
            IEnumerable<MethodModel> methodModels)
        {
            var conventionBuilderList = new List<IEndpointConventionBuilder>();
            foreach (var method1 in methodModels)
            {
                var method = method1;
                var conventionBuilder = endpointRouteBuilder.Map(method.Pattern, method.RequestDelegate);
                conventionBuilder.Add(ep =>
                {
                    ep.DisplayName = "dbConfig - " + method.Pattern.RawText;
                    foreach (var obj in method.Metadata)
                        ep.Metadata.Add(obj);

                    foreach (var attribute in _options.Attributes)
                        ep.Metadata.Add(attribute);
                });
                conventionBuilderList.Add(conventionBuilder);
            }
            return conventionBuilderList;
        }
    }
}
