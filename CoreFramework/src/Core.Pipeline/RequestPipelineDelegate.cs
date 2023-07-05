namespace Core.Pipeline
{
    public delegate Task RequestPipelineDelegate(IRequest request, CancellationToken cancellationToken);
}
