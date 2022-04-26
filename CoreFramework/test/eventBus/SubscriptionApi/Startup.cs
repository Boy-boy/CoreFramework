using Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SubscriptionApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEventBus(options =>
            {
                options.AddConsumers(typeof(Startup).Assembly);
                options.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));
            });

            services.ConfigureServiceCollection<StartupModule>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.BuildApplicationBuilder();
        }
    }
}
