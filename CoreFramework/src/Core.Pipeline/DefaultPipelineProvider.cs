using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Core.Pipeline
{
    public class DefaultPipelineProvider : IPipelineProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPipelineBuilderFactory _pipelineBuilderFactory;

        private readonly ConcurrentDictionary<Type, RequestPipelineDelegate> _requestHandlerDelegates;
        private readonly object _handlerLock = new();

        public DefaultPipelineProvider(
            IServiceProvider serviceProvider,
            IPipelineBuilderFactory pipelineBuilderFactory)
        {
            _serviceProvider = serviceProvider;
            _pipelineBuilderFactory = pipelineBuilderFactory;
            _requestHandlerDelegates = new ConcurrentDictionary<Type, RequestPipelineDelegate>();
        }

        public RequestPipelineDelegate Get<TRequest>() where TRequest : IRequest
        {
            if (_requestHandlerDelegates.TryGetValue(typeof(TRequest), out var handlerDelegate))
                return handlerDelegate;

            lock (_handlerLock)
            {
                var requestHandlers = _serviceProvider.GetRequiredService<IEnumerable<IRequestHandler<TRequest>>>();
                var handlerPipeline = new RequestHandlerPipeline<TRequest>(requestHandlers);
                return BuildPipeline(handlerPipeline);
            }
        }

        private RequestPipelineDelegate BuildPipeline<TRequest>(IPipeline<TRequest> handlerPipeline) where TRequest : IRequest
        {
            if (_requestHandlerDelegates.TryGetValue(typeof(TRequest), out var handlerDelegate))
                return handlerDelegate;

            var pipelines = _serviceProvider.GetRequiredService<IEnumerable<IPipeline<TRequest>>>().ToList();
            pipelines = pipelines.Append(handlerPipeline).ToList();
            pipelines = pipelines.OrderBy(pipeline => PipelinePriorityAttribute.GetPriority(typeof(TRequest), pipeline.GetType())).ToList();

            var pipelineBuilder = _pipelineBuilderFactory.CreateBuilder();

            foreach (var pipeline in pipelines)
            {
                pipelineBuilder.Use(next =>
                {
                    return async (req, cancellationToken) =>
                    {
                        await pipeline.InvokeAsync((TRequest)req, next, cancellationToken);
                    };
                });
            }
            handlerDelegate = pipelineBuilder.Build();
            _requestHandlerDelegates.TryAdd(typeof(TRequest), handlerDelegate);
            return handlerDelegate;
        }
    }
}
