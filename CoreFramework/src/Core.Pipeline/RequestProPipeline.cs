namespace Core.Pipeline
{
    [PipelinePriority(999)]
    public class RequestProPipeline<TRequest> : IPipeline<TRequest>
        where TRequest : IRequest
    {
        private readonly IEnumerable<IRequestProHandler<TRequest>> _proHandlers;

        public RequestProPipeline(IEnumerable<IRequestProHandler<TRequest>> proHandlers)
        {
            _proHandlers = proHandlers.OrderBy(pipeline =>
                HandlerPriorityAttribute.GetPriority(typeof(TRequest), pipeline.GetType()));
        }

        public async Task InvokeAsync(TRequest request, RequestPipelineDelegate next, CancellationToken cancellationToken = default)
        {
            foreach (var proHandler in _proHandlers)
                await proHandler.HandleAsync(request, cancellationToken);
            await next(request, cancellationToken);
        }
    }
}
