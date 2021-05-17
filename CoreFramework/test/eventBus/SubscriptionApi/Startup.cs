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
            #region eventbusʹ�÷�ʽһ
            //services.AddEventBus(options => options.AutoRegistrarHandlersAssemblies = new[] { typeof(Startup).Assembly })
            //    .AddRabbitMq(options =>
            //    {
            //        //������Ϣ��Ӧ��Exchange��Queue���������ã���ʹ��Ĭ�ϵģ�
            //        options.AddSubscribeConfigures(configureOptions =>
            //        {
            //            configureOptions.Add(new RabbitMqSubscribeConfigure(typeof(CustomerEvent),RabbitMqConst.DefaultExchangeName,RabbitMqConst.DefaultQueueName));
            //        });
            //    });
            //services.Configure<RabbitMqOptions>(Configuration.GetSection("RabbitMq"));
            //services.AddRabbitMq();
            #endregion

            #region eventbusʹ�÷�ʽ��
            //services.AddEventBus(options =>
            //{
            //    options.AutoRegistrarHandlersAssemblies = new[] {typeof(Startup).Assembly},
            //    options.AddRabbitMq(actionOptions =>
            //    {
            //        //������Ϣ��Ӧ��Exchange��Queue���������ã���ʹ��Ĭ�ϵģ�
            //        actionOptions.AddSubscribeConfigures(configureOptions =>
            //        {
            //            configureOptions.Add(new RabbitMqSubscribeConfigure(typeof(CustomerEvent),
            //                RabbitMqConst.DefaultExchangeName, RabbitMqConst.DefaultQueueName));
            //        });
            //        //����rabbitMq�����ӵ�ַ
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
