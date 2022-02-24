namespace Core.Pipeline
{
    public interface IPipeline
    {

    }

    public interface IPipeline<in TRequest> : IPipeline where TRequest : IRequest
    {
        Task InvokeAsync(TRequest request, RequestHandlerDelegate next);
    }
}
