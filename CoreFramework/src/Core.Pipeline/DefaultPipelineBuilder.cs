namespace Core.Pipeline
{
    public class DefaultPipelineBuilder : IPipelineBuilder
    {
        private readonly IList<Func<RequestPipelineDelegate, RequestPipelineDelegate>> _components = new List<Func<RequestPipelineDelegate, RequestPipelineDelegate>>();

        public RequestPipelineDelegate Build()
        {
            RequestPipelineDelegate requestDelegate = (_, _) => Task.CompletedTask;

            foreach (Func<RequestPipelineDelegate, RequestPipelineDelegate> func in _components.Reverse())
                requestDelegate = func(requestDelegate);
            return requestDelegate;
        }

        public IPipelineBuilder Use(Func<RequestPipelineDelegate, RequestPipelineDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }
    }

    public static class UseExtensions
    {
        public static IPipelineBuilder Use(
            this IPipelineBuilder app,
            Func<IRequest, Func<Task>, Task> middleware)
        {
            return app.Use(next => (request, cancellationToken) =>
            {
                Task Func() => next(request, cancellationToken);
                return middleware(request, Func);
            });
        }
    }
}
