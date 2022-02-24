namespace Core.Pipeline
{
    public class RequestPipelineBuilder : IPipelineBuilder
    {
        private readonly IList<Func<RequestHandlerDelegate, RequestHandlerDelegate>> _components = new List<Func<RequestHandlerDelegate, RequestHandlerDelegate>>();

        public RequestHandlerDelegate Build()
        {
            RequestHandlerDelegate requestDelegate = _ => Task.CompletedTask;

            foreach (Func<RequestHandlerDelegate, RequestHandlerDelegate> func in _components.Reverse())
                requestDelegate = func(requestDelegate);
            return requestDelegate;
        }

        public IPipelineBuilder Use(Func<RequestHandlerDelegate, RequestHandlerDelegate> middleware)
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
            return app.Use(next => request =>
            {
                Task Func() => next(request);
                return middleware(request, Func);
            });
        }
    }
}
