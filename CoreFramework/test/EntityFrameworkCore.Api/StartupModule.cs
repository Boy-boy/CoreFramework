using Core.EntityFrameworkCore;
using Core.EventBus;
using Core.EventBus.Local;
using Core.EventBus.RabbitMQ;
using Core.EventBus.Storage.EfCore;
using Core.Modularity;
using Core.Modularity.Attribute;
using Core.Uow;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EntityFrameworkCore.Api
{
    [DependsOn(typeof(CoreEfCoreModule)
       , typeof(CoreEventBusLocalModule)
       , typeof(CoreEventBusRabbitMqModule))]
    public class StartupModule : CoreModuleBase
    {
        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void PreConfigureServices(ServiceCollectionContext context)
        {
            //方式一
            // context.Items.Add(nameof(CustomerDbContext), typeof(CustomerDbContext));
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddControllers();

            //方式一
            //context.Services.AddDbContext<CustomerDbContext>(options =>
            //{
            //    options.UseNpgsql(Configuration.GetConnectionString("customer"));
            //});

            //方式二
            context.Services
                .AddDbContextAndEfRepositories<CustomerDbContext>(options =>
            {
                options.UseInMemoryDatabase("customer");
            });

            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddConsumers(typeof(Startup).Assembly);
                // 注册 EF Core outbox / inbox 存储 + Outbox Dispatcher + Inbox 清理
                options.AddEfCoreEventBusStorage<CustomerDbContext>();
            });

            //方式三
            //context.Services
            //    .AddDbContext<CustomerDbContext>(options =>
            //{
            //    options.UseInMemoryDatabase("customer");
            //})
            //    .AddRepositories<CustomerDbContext>();
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

            app.UseUnitOfWork();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
