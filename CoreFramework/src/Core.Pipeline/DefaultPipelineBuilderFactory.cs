namespace Core.Pipeline
{
    public class DefaultPipelineBuilderFactory : IPipelineBuilderFactory
    {
        public IPipelineBuilder CreateBuilder()
        {
            return new DefaultPipelineBuilder();
        }
    }
}
