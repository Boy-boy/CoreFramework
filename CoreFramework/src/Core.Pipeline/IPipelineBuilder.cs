namespace Core.Pipeline
{
    public interface IPipelineBuilder
    {
        IPipelineBuilder Use(Func<RequestHandlerDelegate, RequestHandlerDelegate> middleware);

        RequestHandlerDelegate Build();
    }
}
