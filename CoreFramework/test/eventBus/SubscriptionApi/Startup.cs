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
            #region eventbus使用方式一
            //services.AddEventBus(options => options.AutoRegistrarHandlersAssemblies = new[] { typeof(Startup).Assembly })
            //    .AddRabbitMq(options =>
            //    {
            //        //配置消息对应的Exchange和Queue（若不配置，则使用默认的）
            //        options.AddSubscribeConfigures(configureOptions =>
            //        {
            //            configureOptions.Add(new RabbitMqSubscribeConfigure(typeof(CustomerEvent),RabbitMqConst.DefaultExchangeName,RabbitMqConst.DefaultQueueName));
            //        });
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
