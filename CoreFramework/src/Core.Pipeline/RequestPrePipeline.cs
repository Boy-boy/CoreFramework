namespace Core.Pipeline
{
    [PipelinePriority(-999)]
    public class RequestPrePipeline<TRequest> : IPipeline<TRequest>
        where TRequest : IRequest
    {
        private readonly IEnumerable<IRequestPreHandler<TRequest>> _preHandlers;

        public RequestPrePipeline(IEnumerable<IRequestPreHandler<TRequest>> preHandlers)
        {
            _preHandlers = preHandlers.OrderBy(pipeline =>
                HandlerPriorityAttribute.GetPriority(typeof(TRequest), pipeline.GetType()));
        }

        public async Task InvokeAsync(TRequest request, RequestPipelineDelegate next, CancellationToken cancellationToken = default)
        {
            foreach (var preHandler in _preHandlers)
                await preHandler.HandleAsync(request, cancellationToken);
            await next(request, cancellationToken);
        }
    }
}
