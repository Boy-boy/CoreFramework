
namespace Core.Uow
{
    public interface IUnitOfWorkAccessor
    {
        IUnitOfWork UnitOfWork { get; }
    }
}
