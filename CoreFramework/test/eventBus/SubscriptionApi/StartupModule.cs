using Core.EventBus;
using Core.EventBus.Abstraction;
using Core.EventBus.RabbitMQ;
using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SubscriptionApi.Event;

namespace SubscriptionApi
{
    [DependsOn(typeof(CoreEventBusRabbitMqModule))]
    public class StartupModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void PreConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AutoRegistrarHandlersAssemblies = new[] { typeof(StartupModule).Assembly };
            });
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddControllers();
        }

        public override void Configure(ApplicationBuilderContext context)
        {
            var app = context.ApplicationBuilder;
            var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            eventBus.Subscribe<CustomerEvent, EventHandler>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
