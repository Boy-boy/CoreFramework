namespace Core.Uow
{
    public interface IUnitOfWorkManager
    {
        IUnitOfWork this[string dataBaseName] { get; }

        void TryAddUnitOfWork(string dataBaseName, IUnitOfWork unitOfWork);

        bool TryGetUnitOfWork(string dataBaseName, out IUnitOfWork unitOfWork);

    }
}
