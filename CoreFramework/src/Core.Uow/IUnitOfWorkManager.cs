namespace Core.Uow
{
    public interface IUnitOfWorkManager
    {
        IUnitOfWork Begin();

        IUnitOfWork Begin(UnitOfWorkOptions options);

        Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken = default);

        Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default);
    }
}
