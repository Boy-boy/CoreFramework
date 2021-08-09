namespace Core.Configuration.Dashboard
{
    public interface IMethod
    {
        string ServiceName { get; }

        string Name { get; }

        string FullName { get; }

        string HttpMetadata { get; }
    }
}
