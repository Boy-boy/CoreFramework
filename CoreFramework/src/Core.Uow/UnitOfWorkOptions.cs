using System.Data;

namespace Core.Uow
{
    public class UnitOfWorkOptions
    {
        public bool IsTransactional { get; set; }

        public IsolationLevel? IsolationLevel { get; set; }


        public UnitOfWorkOptions()
        {
        }

        public UnitOfWorkOptions(bool isTransactional = false, IsolationLevel? isolationLevel = null)
        {
            IsTransactional = isTransactional;
            IsolationLevel = isolationLevel;
        }
    }
}
