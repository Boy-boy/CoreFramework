using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Core.Uow
{
    public class UnitOfWorkMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IUnitOfWorkAccessor _unitOfWorkAccessor;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public UnitOfWorkMiddleware(RequestDelegate next,
            IUnitOfWorkAccessor unitOfWorkAccessor,
            IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _unitOfWorkAccessor = unitOfWorkAccessor;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context != null ? context.GetEndpoint() : throw new ArgumentNullException(nameof(context));
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            var unitOfWorkAttribute = endpoint.Metadata.GetOrderedMetadata<UnitOfWorkAttribute>().FirstOrDefault();

            var options = CreateOptions(context, unitOfWorkAttribute);
            _unitOfWorkAccessor.UnitOfWork = CreateUnitOfWork(options);
            await _next(context);
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

        private IUnitOfWork CreateUnitOfWork(UnitOfWorkOptions options)
        {
            var scope = _serviceScopeFactory.CreateScope();
            var uow = ActivatorUtilities.CreateInstance<DefaultUnitOfWork>(scope.ServiceProvider);
            uow.Initialize(options);
            return uow;
        }
    }
}
