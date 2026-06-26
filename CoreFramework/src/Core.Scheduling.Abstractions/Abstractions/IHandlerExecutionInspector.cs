using System.Collections.Generic;
using Core.Scheduling.Models;

namespace Core.Scheduling.Abstractions
{
    /// <summary>
    /// 处理器状态检视(进程内单例),供运维端点 / 健康检查使用。
    /// BG 模式下视图覆盖全部 handler;Quartz 模式下仅反映本节点最近一次执行的快照,
    /// 集群整体视图请直接查询 QRTZ_TRIGGERS / QRTZ_FIRED_TRIGGERS。
    /// </summary>
    public interface IHandlerExecutionInspector
    {
        /// <summary>按编码获取状态,不存在返回 <see langword="null"/>。</summary>
        HandlerState GetState(string handlerCode);

        /// <summary>获取全部已知 handler 的当前状态。</summary>
        IReadOnlyCollection<HandlerState> GetAllStates();
    }
}
