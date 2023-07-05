using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Pipeline
{
    public class DefaultPipelineProvider : IPipelineProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPipelineBuilderFactory _pipelineBuilderFactory;

        private readonly ConcurrentDictionary<Type, RequestHandlerDelegate> _requestHandlerDelegates;
        private readonly object _handlerLock = new();

        public DefaultPipelineProvider(
            IServiceProvider serviceProvider,
            IPipelineBuilderFactory pipelineBuilderFactory)
        {
            _serviceProvider = serviceProvider;
            _pipelineBuilderFactory = pipelineBuilderFactory;
            _requestHandlerDelegates = new ConcurrentDictionary<Type, RequestHandlerDelegate>();
        }

        public RequestHandlerDelegate Get<TRequest>() where TRequest : IRequest
        {
            //TODO: 此处管道只构建一次，若Pipeline构造函数注入依赖，是否存在问题？
            if (_requestHandlerDelegates.TryGetValue(typeof(TRequest), out var handlerDelegate))
                return handlerDelegate;

            lock (_handlerLock)
            {
                var pipelines = _serviceProvider.GetRequiredService<IEnumerable<IPipeline<TRequest>>>();

                pipelines = pipelines.OrderBy(pipeline => PipelinePriorityAttribute.GetPriority(typeof(TRequest), pipeline.GetType()));

                var pipelineBuilder = _pipelineBuilderFactory.CreateBuilder();

                foreach (var pipeline in pipelines)
                {
                    pipelineBuilder.Use(next =>
                    {
                        return async req =>
                        {
                            await pipeline.InvokeAsync((TRequest)req, next);
                        };
                    });
                }
                handlerDelegate = pipelineBuilder.Build();
                _requestHandlerDelegates.TryAdd(typeof(TRequest), handlerDelegate);
                return handlerDelegate;
            }
        }
    }
}
