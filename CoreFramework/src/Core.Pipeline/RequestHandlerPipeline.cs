namespace Core.Pipeline
{
    [PipelinePriority(998)]
    public class RequestHandlerPipeline<TRequest> : IPipeline<TRequest>
        where TRequest : IRequest
    {
        private readonly IEnumerable<IRequestHandler<TRequest>> _requestHandlers;

        public RequestHandlerPipeline(IEnumerable<IRequestHandler<TRequest>> requestHandlers)
        {
            requestHandlers ??= new List<IRequestHandler<TRequest>>();
            _requestHandlers = requestHandlers.OrderBy(pipeline =>
              HandlerPriorityAttribute.GetPriority(typeof(TRequest), pipeline.GetType()));
        }

        public async Task InvokeAsync(TRequest request, RequestPipelineDelegate next, CancellationToken cancellationToken = default)
        {
            foreach (var requestHandler in _requestHandlers)
            {
                await requestHandler.HandleAsync(request, cancellationToken);
            }
            await next(request, cancellationToken);
        }
    }
}
