using System.Reflection;

namespace Core.Pipeline
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class PipelinePriorityAttribute : Attribute
    {
        public virtual int Priority { get; }

        public PipelinePriorityAttribute()
        : this(0)
        {
        }

        public PipelinePriorityAttribute(int priority)
        {
            Priority = priority;
        }

        public static int GetPriority(Type pipelineHandlerType)
        {
            if (pipelineHandlerType == null)
            {
                throw new ArgumentNullException(nameof(pipelineHandlerType));
            }
            var pipelineMethods = pipelineHandlerType
                .GetMethods()
                .Where(x => x.Name == "InvokeAsync");
            foreach (var method in pipelineMethods)
            {
                var methodParameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
                if (methodParameterTypes.Length != 2 || !typeof(IRequest).GetTypeInfo().IsAssignableFrom(methodParameterTypes[0]) || methodParameterTypes[1].FullName != "Core.Pipeline.RequestHandlerDelegate") continue;
                var methodPriorityAttributes = method.GetCustomAttributes(true).OfType<PipelinePriorityAttribute>().ToList();
                if (methodPriorityAttributes.Any())
                {
                    return methodPriorityAttributes.First().Priority;
                }
            }
            return pipelineHandlerType.GetCustomAttributes(true).OfType<PipelinePriorityAttribute>().FirstOrDefault()?.Priority ?? 0;
        }
    }
}
