using Microsoft.AspNetCore.Http;

namespace Core.Uow
{
    public class UnitOfWorkMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IUnitOfWorkAccessor _unitOfWorkAccessor;

        public UnitOfWorkMiddleware(RequestDelegate next,
            IUnitOfWorkAccessor unitOfWorkAccessor)
        {
            _next = next;
            _unitOfWorkAccessor = unitOfWorkAccessor;
        }

        public async Task InvokeAsync(HttpContext context, IUnitOfWorkManager unitOfWorkManager)
        {
            var endpoint = context != null ? context.GetEndpoint() : throw new ArgumentNullException(nameof(context));
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            var unitOfWorkAttribute = endpoint.Metadata.GetOrderedMetadata<UnitOfWorkAttribute>().FirstOrDefault();
            var options = CreateOptions(context, unitOfWorkAttribute);
            var uow = unitOfWorkManager.Begin(options);

            try
            {
                await _next(context);
                await uow.CommitAsync(context.RequestAborted);
            }
            catch
            {
                await uow.RollbackAsync();
                throw;
            }
            finally
            {
                await uow.DisposeAsync();
                _unitOfWorkAccessor.UnitOfWork = null;
            }
        }

        private UnitOfWorkOptions CreateOptions(HttpContext context, UnitOfWorkAttribute unitOfWorkAttribute)
        {
            var options = new UnitOfWorkOptions();

            unitOfWorkAttribute?.SetOptions(options);

            if (unitOfWorkAttribute?.IsTransactional == null)
            {
                options.IsTransactional = !string.Equals(context.Request.Method, HttpMethod.Get.Method,
                    StringComparison.OrdinalIgnoreCase);
            }

            return options;
        }
    }
}
