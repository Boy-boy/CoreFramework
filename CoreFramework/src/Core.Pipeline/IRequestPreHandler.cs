namespace Core.Pipeline
{
    public interface IRequestPreHandler
    {
    }

    public interface IRequestPreHandler<in TRequest> : IRequestHandler
    where TRequest : IRequest
    {
        Task HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}
