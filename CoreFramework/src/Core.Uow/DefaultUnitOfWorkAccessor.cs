namespace Core.Uow
{
    public class DefaultUnitOfWorkAccessor : IUnitOfWorkAccessor
    {
        private readonly AsyncLocal<IUnitOfWork> _unitOfWorkAsyncLocal = new();

        public IUnitOfWork UnitOfWork
        {
            get => _unitOfWorkAsyncLocal.Value;
            set => _unitOfWorkAsyncLocal.Value = value;
        }
    }
}
