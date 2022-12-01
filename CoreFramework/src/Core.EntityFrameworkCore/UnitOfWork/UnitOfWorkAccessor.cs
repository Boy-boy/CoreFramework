using Core.Uow;
using System.Threading;

namespace Core.EntityFrameworkCore.UnitOfWork
{
    public class UnitOfWorkAccessor : IUnitOfWorkAccessor
    {
        private static readonly AsyncLocal<IUnitOfWork> UnitOfWorkAsyncLocal = new();
        public IUnitOfWork UnitOfWork
        {
            get
            {
                var uow = UnitOfWorkAsyncLocal.Value;
                if (uow != null)
                {
                    return uow;
                }

                uow = UnitOfWorkAsyncLocal.Value = new EfCoreUnitOfWork();
                return uow;
            }
        }
    }
}
