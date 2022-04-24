using System;
using Microsoft.Extensions.DependencyInjection;

namespace Test
{

    public class BaseTest
    {
        protected IServiceProvider ServiceProvider { get; set; }
        public BaseTest()
        {
            ConfigureServices();
        }


        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddPipeline(typeof(BaseTest).Assembly);

            services.AddAmazonS3(configActions =>
            {

            });

            services.AddEmailClient(options =>
            {
                options.Host = "smtp.qq.com";
                options.Port = 465;
                options.ClientId = "*@qq.com";
                options.ClientSecret = "cglvafztpzfncajf";
                options.AddPostgreSql(actionOptions =>
                {
                    actionOptions.DbConnection = "Host=81.69.227.172;Port=31432;Database=customer;Username=postgres;Password=gb123456";
                    actionOptions.DbSchema = "Email";
                    actionOptions.DbTable = "PublishMessage";
                });
            });

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}