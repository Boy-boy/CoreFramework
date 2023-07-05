using System;
using Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;

namespace Test
{
    [TestClass]
    public class PipelineTest : BaseTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var pipeline = ServiceProvider.GetRequiredService<IPipelineProvider>();
            pipeline.Get<PipelineModel>()(new PipelineModel(), default);
        }
    }

    public class PipelineHandler : IPipeline<PipelineModel>
    {
        public async Task InvokeAsync(PipelineModel request, RequestPipelineDelegate next, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("自定义管道");
            await next(request, cancellationToken);
        }
    }

    public class PreRequestHandler : IRequestPreHandler<PipelineModel>
    {
        public async Task HandleAsync(PipelineModel request, CancellationToken cancellationToken)
        {
            Console.WriteLine("第一个管道执行处理器");
            await Task.CompletedTask;
        }
    }

    public class ProRequestHandler : IRequestProHandler<PipelineModel>
    {
        public async Task HandleAsync(PipelineModel request, CancellationToken cancellationToken)
        {
            Console.WriteLine("最后管道执行处理器");
            await Task.CompletedTask;
        }

    }

    public class FistRequestHandler : IRequestHandler<PipelineModel>
    {
        public async Task HandleAsync(PipelineModel request, CancellationToken cancellationToken)
        {
            Console.WriteLine("第一个管道处理器");
            await Task.CompletedTask;
        }

    }

    public class TwoRequestHandler : IRequestHandler<PipelineModel>
    {
        public async Task HandleAsync(PipelineModel request, CancellationToken cancellationToken)
        {
            Console.WriteLine("第二个管道处理器");
            await Task.CompletedTask;
        }
    }

    public class ThreeRequestHandler : IRequestHandler<PipelineModel>
    {
        public async Task HandleAsync(PipelineModel request, CancellationToken cancellationToken)
        {
            Console.WriteLine("第三个管道处理器");
            await Task.CompletedTask;
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