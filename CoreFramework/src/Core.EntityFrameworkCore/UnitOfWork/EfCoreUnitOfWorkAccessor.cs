using Core.Uow;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace Core.EntityFrameworkCore.UnitOfWork
{
    public class EfCoreUnitOfWorkAccessor : IUnitOfWorkAccessor
    {
        private static readonly AsyncLocal<IUnitOfWork> UnitOfWorkAsyncLocal = new();

        private readonly IServiceScopeFactory _scopeFactory;

        public EfCoreUnitOfWorkAccessor(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public IUnitOfWork UnitOfWork
        {
            get
            {
                var uow = UnitOfWorkAsyncLocal.Value;
                if (uow != null)
                    return uow;
                uow = CreateUnitOfWork();
                UnitOfWorkAsyncLocal.Value = uow;

                return uow;
            }
            set => UnitOfWorkAsyncLocal.Value = value;
        }

        private IUnitOfWork CreateUnitOfWork()
        {
            var scope = _scopeFactory.CreateScope();
            var uow = new EfCoreUnitOfWork(scope.ServiceProvider);
            return uow;
        }
    }
}
