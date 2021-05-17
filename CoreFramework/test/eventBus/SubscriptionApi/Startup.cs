using Core.EventBus.RabbitMQ;
using Core.Modularity;
using Core.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionApi.Event;

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

            #region eventbus使用方式二
            //services.AddEventBus(options =>
            //{
            //    options.AutoRegistrarHandlersAssemblies = new[] {typeof(Startup).Assembly},
            //    options.AddRabbitMq(actionOptions =>
            //    {
            //        //配置消息对应的Exchange和Queue（若不配置，则使用默认的）
            //        actionOptions.AddSubscribeConfigures(configureOptions =>
            //        {
            //            configureOptions.Add(new RabbitMqSubscribeConfigure(typeof(CustomerEvent),
            //                RabbitMqConst.DefaultExchangeName, RabbitMqConst.DefaultQueueName));
            //        });
            //        //配置rabbitMq的连接地址
            //        actionOptions.RabbitMqOptions = rabbitOptions =>
            //        {
            //            rabbitOptions.Connection=new RabbitMqConnectionConfigure();
            //        };
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
