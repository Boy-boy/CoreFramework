using Core.Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class AmazonS3Test : BaseTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var amazonS3ClientFactory = ServiceProvider.GetRequiredService<IAmazonS3ClientFactory>();
            var amazonS3Client = amazonS3ClientFactory.CreateClient();
            //TODO:根据业务自定义使用amazonS3Client
        }
    }
}