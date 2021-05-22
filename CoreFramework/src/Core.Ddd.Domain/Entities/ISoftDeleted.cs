namespace Core.Ddd.Domain.Entities
{
    public interface ISoftDeleted
    {
        /// <summary>
        /// 软删除
        /// </summary>
        public bool IsDeleted { get; set; }

    }
}
