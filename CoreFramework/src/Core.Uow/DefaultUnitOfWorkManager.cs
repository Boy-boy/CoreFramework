using System;
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
        public IUnitOfWork Begin()
        {
            var uow = _unitOfWorkAccessor.UnitOfWork ?? (_unitOfWorkAccessor.UnitOfWork = CreateUnitOfWork());
            return uow;
        }

        private IUnitOfWork CreateUnitOfWork()
        {
            var uow = ActivatorUtilities.CreateInstance<DefaultUnitOfWork>(_serviceProvider);
            uow.Initialize(new UnitOfWorkOptions());
            return uow;
        }
    }
}
