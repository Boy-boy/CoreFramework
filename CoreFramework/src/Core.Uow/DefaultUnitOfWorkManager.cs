using Microsoft.Extensions.DependencyInjection;

namespace Core.Uow
{
    public class DefaultUnitOfWorkManager : IUnitOfWorkManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWorkAccessor _unitOfWorkAccessor;

        public DefaultUnitOfWorkManager(IServiceProvider serviceProvider,
            IUnitOfWorkAccessor unitOfWorkAccessor)
        {
            _serviceProvider = serviceProvider;
            _unitOfWorkAccessor = unitOfWorkAccessor;
        }

        public IUnitOfWork Begin() => Begin(new UnitOfWorkOptions());

        public IUnitOfWork Begin(UnitOfWorkOptions options)
        {
            options ??= new UnitOfWorkOptions();

            var existing = _unitOfWorkAccessor.UnitOfWork;
            if (existing != null)
            {
                if (options.IsTransactional && !existing.Options.IsTransactional)
                {
                    throw new InvalidOperationException(
                        "嵌套 UnitOfWork 需要事务，但外层 UnitOfWork 不是事务性的。请在外层调用方显式开启事务。");
                }
                return new ChildUnitOfWork(existing);
            }

            var uow = CreateUnitOfWork(options);
            _unitOfWorkAccessor.UnitOfWork = uow;
            return uow;
        }

        public Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Begin());

        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
            => Task.FromResult(Begin(options));

        private IUnitOfWork CreateUnitOfWork(UnitOfWorkOptions options)
        {
            var uow = ActivatorUtilities.CreateInstance<DefaultUnitOfWork>(_serviceProvider);
            uow.Initialize(options);
            return uow;
        }
    }
}
