using Core.Modularity;
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
            //services.AddEventBus()
            //    .AddRabbitMq(options =>
            //    {
            //        //配置消息对应的Exchange（若不配置，则使用默认的）
            //        options.AddPublishConfigure(configureOptions =>
            //        {
            //            configureOptions.ExchangeName = RabbitMqConst.DefaultExchangeName;
            //        });
            //    })
            //    .AddSqlServer(options =>
            //    {
            //        options.ConnectionString = Configuration.GetConnectionString("customer");
            //    });
            //services.Configure<RabbitMqOptions>(Configuration.GetSection("RabbitMq"));
            //services.AddRabbitMq();
            #endregion

            services.ConfigureServiceCollection<StartupModule>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.BuildApplicationBuilder();
        }
    }
}
