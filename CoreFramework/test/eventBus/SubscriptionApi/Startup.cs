using Core.Modularity;
using Core.RabbitMQ;
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
            //services.AddEventBus(options =>
            //{
            //    options.HandlersAssemblies = new[] { typeof(Startup).Assembly };
            //    options.AddRabbitMq(rabbitOptions =>
            //    {
            //        rabbitOptions.ExchangeName = "demo";
            //        rabbitOptions.RabbitMqConnection = new RabbitMqConnectionConfigure();
            //    });
            //});

            services.ConfigureServiceCollection<StartupModule>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.BuildApplicationBuilder();
        }
    }
}
