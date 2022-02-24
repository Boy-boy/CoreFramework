namespace Core.Pipeline
{
    public interface IPipelineProvider
    {
        RequestHandlerDelegate Get<TRequest>() where TRequest : IRequest;
    }
}
