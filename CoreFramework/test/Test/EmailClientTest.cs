using System.Collections.Generic;
using System.IO;
using System.Threading;
using Core.EmailClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class EmailClientTest : BaseTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var emailClient = ServiceProvider.GetRequiredService<IEmailClient>();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "picture.gif");
            var fileByte = File.ReadAllBytes(filePath);

            var filePath1 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "email_content.html");
            var emailContent= File.ReadAllText(filePath1);

            emailClient.SendAsync(new MailBodyEntity
            {
                CreationUserId = "1",
                Subject = "haha",
                Sender = "gaobo",
                SenderAddress = "*@qq.com",
                Recipients = new List<string>()
                {
                    "*@qq.com"
                },
                Body = emailContent,
                BodyType = MailTextFormat.Html,
                MailFiles = new List<MailFile>()
                {
                    new MailFile("picture.gif",fileByte)
                },
                LinkedResources = new List<MailFile>()
                {
                    new MailFile("picture.gif",fileByte)
                }
            }).GetAwaiter().GetResult();
            Thread.Sleep(2000);
        }
    }
}