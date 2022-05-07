using Core.EmailClient;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EmailClientServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailClient(this IServiceCollection services,
            Action<EmailClientOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            var options = new EmailClientOptions();
            configureOptions.Invoke(options);
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }

            services.AddLogging();
            services.AddOptions();
            services.Configure(configureOptions);
            services.TryAddSingleton<IEmailClient, DefaultEmailClient>();
            services.AddHostedService<EmailClientBackgroundService>();

            return services;
        }
    }
}
