namespace Core.Configuration.Dashboard
{
    public class Method<TRequest, TResponse> : IMethod
    {
        public Method(string serviceName, string name,string httpMetadata)
        {
            ServiceName = serviceName;
            Name = name;
            HttpMetadata = httpMetadata;
            FullName = GetFullName(ServiceName, Name);
        }

        public string ServiceName { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public string HttpMetadata { get; set; }

        internal static string GetFullName(string serviceName, string methodName)
        {
            return "/" + serviceName + "/" + methodName;
        }
    }
}
