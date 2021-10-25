namespace Core.Infrastructure.Recursive
{
    public interface IRecursiveModel
    {
        public object Id { get; set; }

        /// <summary>
        ///  若为根节点，ParentId必须为null
        /// </summary>
        public object ParentId { get; set; }
    }
}
