namespace Core.Uow
{
    public interface IUnitOfWorkManager
    {
        IUnitOfWork Begin();

        Task<IUnitOfWork> BeginAsync();
    }
}
