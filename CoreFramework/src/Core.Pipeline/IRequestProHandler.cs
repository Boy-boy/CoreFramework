namespace Core.Pipeline
{
    public interface IRequestProHandler
    {
    }

    public interface IRequestProHandler<in TRequest> : IRequestProHandler
    where TRequest : IRequest
    {
        Task HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}
