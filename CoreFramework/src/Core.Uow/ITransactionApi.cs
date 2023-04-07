using System.Threading.Tasks;

namespace Core.Uow
{
    public interface ITransactionApi
    {
        Task CommitAsync();
    }
}
