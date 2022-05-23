using Microsoft.Extensions.DependencyInjection;

namespace Core.EmailClient
{
    public class EmailClientOptions
    {
        public EmailClientOptions()
        {
            Extensions = new List<IEmailClientOptionsExtensions>();
        }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool UseSsl { get; set; } = true;

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public List<IEmailClientOptionsExtensions> Extensions { get; set; }
    }

    public static class EventBusOptionsExtensions
    {
        public static void AddExtensions(this EmailClientOptions options, IEmailClientOptionsExtensions eventBusOptionExtensions)
        {
            if (eventBusOptionExtensions == null)
                throw new AggregateException(nameof(eventBusOptionExtensions));
            options.Extensions.Add(eventBusOptionExtensions);
        }

        public static void Configure(this EmailClientOptions options, IServiceCollection services)
        {
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }
        }
    }
}
