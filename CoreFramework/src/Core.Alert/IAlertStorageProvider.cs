namespace Core.Alert
{
    /// <summary>
    /// 告警状态存储抽象。
    /// 仅暴露最小 CRUD 能力，不承诺跨调用原子性；进程内串行由 <see cref="AlertEscalationSession"/> 负责。
    /// </summary>
    public interface IAlertStorageProvider
    {
        /// <summary>
        /// 读取指定会话的当前状态；不存在时返回 null。
        /// </summary>
        Task<AlertSessionState> GetAsync(string sessionKey);

        /// <summary>
        /// 保存或覆盖指定会话的当前状态。
        /// </summary>
        Task SetAsync(string sessionKey, AlertSessionState state);

        /// <summary>
        /// 清理指定会话状态。
        /// </summary>
        Task RemoveAsync(string sessionKey);
    }
}
