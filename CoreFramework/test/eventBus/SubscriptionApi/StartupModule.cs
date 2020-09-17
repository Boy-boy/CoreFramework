using Core.EventBus.Abstraction;
using Core.EventBus.RabbitMQ;
using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SubscriptionApi.Event;

namespace SubscriptionApi
{
    [DependsOn(typeof(CoreEventBusRabbitMqModule))]
    public class StartupModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddControllers();
            context.Services.AddScoped<EventHandler>();
        }

        public override void Configure(ConfigureApplicationContext context)
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
