namespace Core.Threading.Tasks
{
    public interface IConcurrentResult
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        string TaskName { get; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 任务完成率
        /// </summary>
        double FinishingRate { get; }
    }
}
