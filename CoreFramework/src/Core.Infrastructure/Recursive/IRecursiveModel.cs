namespace Core.Infrastructure.Recursive
{
    public interface IRecursiveModel
    {
        public object Id { get; set; }

        public object ParentId { get; set; }
    }
}
