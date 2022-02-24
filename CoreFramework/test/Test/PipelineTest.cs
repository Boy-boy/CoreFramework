using System;
using Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Test
{
    [TestClass]
    public class PipelineTest : BaseTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var pipeline = ServiceProvider.GetRequiredService<IPipelineProvider>();
            pipeline.Get<PipelineModel>()(new PipelineModel());
        }
    }

    public class FistPipeline : IPipeline<PipelineModel>
    {
        public async Task InvokeAsync(PipelineModel request, RequestHandlerDelegate next)
        {
            Console.WriteLine("第一个管道处理器");
            await next(request);
        }
    }

    public class TwoPipeline : IPipeline<PipelineModel>
    {
        public async Task InvokeAsync(PipelineModel request, RequestHandlerDelegate next)
        {
            Console.WriteLine("第二个管道处理器");
            await next(request);
        }
    }

    public class ThreePipeline : IPipeline<PipelineModel>
    {
        public async Task InvokeAsync(PipelineModel request, RequestHandlerDelegate next)
        {
            Console.WriteLine("第三个管道处理器");
            await next(request);
        }
    }

    public class PipelineModel : IRequest
    {
        public IEnumerable<KeyValuePair<Type, object>> Features { get; set; }

        public PipelineModel()
        {
            Features = new List<KeyValuePair<Type, object>>();
        }

    }
}