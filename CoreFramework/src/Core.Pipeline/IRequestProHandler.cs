namespace Core.Pipeline
{
    public interface IRequestProHandler
    {
    }

    public interface IRequestProHandler<in TRequest> : IRequestHandler
    where TRequest : IRequest
    {
        Task HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}
