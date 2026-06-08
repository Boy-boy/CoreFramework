namespace Core.Alert
{
    /// <summary>
    /// 告警状态在存储层中的持久化快照。
    /// </summary>
    public class AlertSessionState
    {
        /// <summary>
        /// 当前状态所属业务日，仅使用 Date 部分。
        /// </summary>
        public DateTime BusinessDay { get; set; }

        /// <summary>
        /// 本轮升级计划的总档数。
        /// </summary>
        public int TotalSlots { get; set; }

        /// <summary>
        /// 剩余尚未触发的升级档位，按触发时刻升序排列。
        /// </summary>
        public List<PendingAlertState> Pending { get; set; } = new();
    }

    /// <summary>
    /// 单个待触发档位的快照。
    /// </summary>
    public class PendingAlertState
    {
        /// <summary>
        /// 升级档位序号，从 1 开始。
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// 该档位的绝对触发时刻。
        /// </summary>
        public DateTime FireAt { get; set; }
    }
}
