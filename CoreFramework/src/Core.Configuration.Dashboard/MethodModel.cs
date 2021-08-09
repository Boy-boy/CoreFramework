using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using System.Collections.Generic;

namespace Core.Configuration.Dashboard
{
    public class MethodModel
    {
        public MethodModel(
            RoutePattern pattern,
            IList<object> metadata,
            RequestDelegate requestDelegate)
        {
            Pattern = pattern;
            Metadata = metadata;
            RequestDelegate = requestDelegate;
        }
        public RoutePattern Pattern { get; }

        public IList<object> Metadata { get; }

        public RequestDelegate RequestDelegate { get; }
    }
}
