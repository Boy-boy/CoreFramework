namespace Core.Ddd.Domain.Entities
{
    public class Entity : IEntity
    {
    }

    public class Entity<TKey> : Entity, IEntity<TKey>
    {
        public TKey Id { get; set; }
    }
}
