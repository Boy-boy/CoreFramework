using System.Threading;

namespace Core.EventBus.Transaction
{
    public class TransactionAccessor : ITransactionAccessor
    {
        private static readonly AsyncLocal<TransactionHolder> TransactionAsyncLocal = new();

        public ITransaction Transaction
        {
            get => TransactionAsyncLocal.Value?.Transaction;
            set
            {
                var holder = TransactionAsyncLocal.Value;
                if (holder != null)
                {
                    holder.Transaction = null;
                }

                if (value != null)
                {
                    TransactionAsyncLocal.Value = new TransactionHolder { Transaction = value };
                }
            }
        }

        private class TransactionHolder
        {
            public ITransaction Transaction;
        }
    }
}
