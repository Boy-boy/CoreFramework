namespace Core.Pipeline
{
    public interface IRequestHandler
    {
    }

    public interface IRequestHandler<in TRequest> : IRequestHandler
    where TRequest : IRequest
    {
        Task HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}
