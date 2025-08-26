namespace Core.Uow
{
    public interface ITransactionApi
    {
        Task CommitAsync();

        Task RollbackAsync();
    }
}
