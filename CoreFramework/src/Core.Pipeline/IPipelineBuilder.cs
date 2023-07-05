namespace Core.Pipeline
{
    public interface IPipelineBuilder
    {
        IPipelineBuilder Use(Func<RequestPipelineDelegate, RequestPipelineDelegate> middleware);

        RequestPipelineDelegate Build();
    }
}
