
namespace Core.Uow
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork CreateUow();
    }
}
