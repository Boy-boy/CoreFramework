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

        public static int GetPriority(Type messageType, Type pipelineHandlerType)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }

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
                if (methodParameterTypes.Length != 3 || messageType != methodParameterTypes[0] || typeof(RequestPipelineDelegate) != methodParameterTypes[1] || typeof(CancellationToken) != methodParameterTypes[2])
                    continue;
                var methodPriorityAttribute = method.GetCustomAttributes(true).OfType<PipelinePriorityAttribute>().FirstOrDefault();
                if (methodPriorityAttribute != null)
                {
                    return methodPriorityAttribute.Priority;
                }
            }
            return pipelineHandlerType.GetCustomAttributes(true).OfType<PipelinePriorityAttribute>().FirstOrDefault()?.Priority ?? 0;
        }
    }
}
