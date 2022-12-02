using System.Threading;

namespace Core.Uow
{
    public class UnitOfWorkAccessor : IUnitOfWorkAccessor
    {
        private static readonly AsyncLocal<IUnitOfWork> UnitOfWorkAsyncLocal = new();
        public IUnitOfWork UnitOfWork
        {
            get
            {
                var uow = UnitOfWorkAsyncLocal.Value;
                return uow;
            }
            set => UnitOfWorkAsyncLocal.Value = value;
        }
    }
}
