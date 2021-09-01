using Core.EventBus.PostgreSql;
using Core.Modularity;
using Core.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PublishApi
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
            #region eventbus使用方式一
            //services.AddEventBus(options =>
            //{
            //    options.AddRabbitMq(rabbitOptions =>
            //    {
            //        rabbitOptions.ExchangeName = "demo";
            //        rabbitOptions.RabbitMqConnection = new RabbitMqConnectionConfigure();
            //    });

            //    options.AddPostgreSql(pgOptions =>
            //    {
            //        pgOptions.DbConnection = "demo";
            //        pgOptions.DbSchema = "demo";
            //        pgOptions.DbTable = "demo";
            //    });
            //});
            #endregion

            services.ConfigureServiceCollection<StartupModule>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.BuildApplicationBuilder();
        }
    }
}
