namespace Core.Ddd.Domain.Entities
{
    public interface IEntity
    {
    }

    public interface IEntity<TKey> : IEntity
    {
        TKey Id { get; set; }
    }
}
