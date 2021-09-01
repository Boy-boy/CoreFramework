using Core.EventBus.PostgreSql;
using Core.EventBus.RabbitMQ;
using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PublishApi
{
    [DependsOn(
        typeof(CoreEventBusRabbitMqModule)
        , typeof(CoreEventBusPostgreSqlModule))]
    public class StartupModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddControllers();

            var rabbitMqConnection = Configuration.GetSection("RabbitMq:Connection").Get<RabbitMqConnectionConfigure>();
            context.Services.Configure<EventBusRabbitMqOptions>(options =>
            {
                options.RabbitMqConnection = rabbitMqConnection;
            });
            context.Services.Configure<EventBusPostgreSqlOptions>(options =>
            {
                options.DbConnection = Configuration.GetConnectionString("customer");
            });
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
