using FluentValidation;
using MediatR.Pipeline;

namespace Core.Application
{
    /// <summary>
    /// 验证请求参数
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public class ValidationRequestPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
        where TRequest : notnull
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationRequestPreProcessor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            var validator = _serviceProvider.GetService(typeof(IValidator<TRequest>));
            if (validator == null)
                return Task.CompletedTask;
            var validationResult = ((IValidator<TRequest>)validator).Validate(request);
            if (validationResult.IsValid)
                return Task.CompletedTask;

            var errorMessages = validationResult.Errors.Select(q => q.ErrorMessage).ToArray();
            throw new Exceptions.ValidationException(string.Join(",  ", errorMessages));
        }
    }
}
