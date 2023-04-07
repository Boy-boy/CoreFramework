using System.Threading;

namespace Core.Uow
{
    public class DefaultUnitOfWorkAccessor : IUnitOfWorkAccessor
    {
        private static readonly AsyncLocal<IUnitOfWork> UnitOfWorkAsyncLocal = new();

        public IUnitOfWork UnitOfWork
        {
            get => UnitOfWorkAsyncLocal.Value;
            set => UnitOfWorkAsyncLocal.Value = value;
        }
    }
}
