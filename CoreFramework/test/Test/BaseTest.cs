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
            var service = new ServiceCollection();

            service.AddPipeline(typeof(BaseTest).Assembly);

            service.AddAmazonS3(configActions =>
            {

            });

            ServiceProvider = service.BuildServiceProvider();
        }
    }
}