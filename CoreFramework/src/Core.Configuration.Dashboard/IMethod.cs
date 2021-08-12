using System;
using System.Collections.Generic;
using System.Reflection;

namespace Core.Configuration.Dashboard
{
    public interface IMethod
    {
        string MethodName { get; }

        Type MethodParameter { get; }

        string HttpMetadata { get; }

        string RouteTemplate { get; }

        List<object> MethodMetadata { get; }

        MethodInfo MethodInvoke { get; }
    }

}
