using System;
using Core.Uow;

namespace Core.EntityFrameworkCore.UnitOfWork
{
    public class EfCoreUnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWorkAccessor _unitOfWorkAccessor;

        public EfCoreUnitOfWorkFactory(IServiceProvider serviceProvider,
            IUnitOfWorkAccessor unitOfWorkAccessor)
        {
            _serviceProvider = serviceProvider;
            _unitOfWorkAccessor = unitOfWorkAccessor;
        }

        public IUnitOfWork CreateUow()
        {
            var uow = new EfCoreUnitOfWork(_serviceProvider);
            _unitOfWorkAccessor.UnitOfWork = uow;
            return uow;
        }
    }
}
