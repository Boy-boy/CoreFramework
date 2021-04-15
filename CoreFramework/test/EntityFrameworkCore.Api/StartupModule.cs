using Core.EntityFrameworkCore;
using Core.EventBus.RabbitMQ;
using Core.EventBus.SqlServer;
using Core.Modularity;
using Core.Modularity.Attribute;
using EntityFrameworkCore.Api.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EntityFrameworkCore.Api
{
    [DependsOn(typeof(CoreEfCoreModule),
        typeof(CoreEventBusRabbitMqModule)
       /* typeof(CoreEventBusSqlServerModule)*/)]
    public class StartupModule : CoreModuleBase
    {
        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void PreConfigureServices(ServiceCollectionContext context)
        {
            context.Items.Add(nameof(CustomerDbContext), typeof(CustomerDbContext));

            //方式一
            context.Services.AddDbContext<CustomerDbContext>(options =>
            {
                options.UseInMemoryDatabase("customer");
            });
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddControllers();

            //方式二
            //context.Services.AddDbContextAndEfRepositories<CustomerDbContext>(options =>
            //{
            //    options.UseInMemoryDatabase("customer");
            //});
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
