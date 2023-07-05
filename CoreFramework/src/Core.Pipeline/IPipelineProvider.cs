namespace Core.Pipeline
{
    public interface IPipelineProvider
    {
        RequestPipelineDelegate Get<TRequest>() where TRequest : IRequest;
    }
}
