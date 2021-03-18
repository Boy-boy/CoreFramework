namespace Core.EventBus.Transaction
{
    public interface ITransactionAccessor
    {
       ITransaction Transaction { get; set; }
    }
}
