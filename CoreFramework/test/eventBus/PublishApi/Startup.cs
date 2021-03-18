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
            #region eventbusʹ�÷�ʽһ
            //services.AddEventBus()
            //    .AddRabbitMq(options =>
            //    {
            //        //������Ϣ��Ӧ��Exchange���������ã���ʹ��Ĭ�ϵģ�
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
