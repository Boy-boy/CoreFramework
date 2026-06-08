namespace Core.Alert
{
    /// <summary>
    /// 告警升级状态机的一次决策结果。
    /// 调用方据此决定本轮是否真正发送告警，以及当前位于第几档升级。
    /// </summary>
    public readonly struct AlertEscalationDecision
    {
        public static readonly AlertEscalationDecision Silent = new AlertEscalationDecision(false, 0, 0);

        private AlertEscalationDecision(bool shouldFire, int sequence, int total)
        {
            ShouldFire = shouldFire;
            Sequence = sequence;
            Total = total;
        }

        /// <summary>
        /// 是否应在本轮触发告警。
        /// </summary>
        public bool ShouldFire { get; }

        /// <summary>
        /// 当前触发的是第几档升级，从 1 开始。
        /// </summary>
        public int Sequence { get; }

        /// <summary>
        /// 本轮升级计划的总档数。
        /// </summary>
        public int Total { get; }

        public static AlertEscalationDecision Fire(int sequence, int total)
            => new AlertEscalationDecision(true, sequence, total);
    }
}
