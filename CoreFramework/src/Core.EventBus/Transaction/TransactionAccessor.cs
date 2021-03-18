using System.Threading;

namespace Core.EventBus.Transaction
{
    public class TransactionAccessor : ITransactionAccessor
    {
        private readonly AsyncLocal<ITransaction> _transaction = new AsyncLocal<ITransaction>();

        public ITransaction Transaction
        {
            get => _transaction.Value;
            set
            {
                if (value != null)
                {
                    _transaction.Value = value;
                }
            }
        }
    }
}
