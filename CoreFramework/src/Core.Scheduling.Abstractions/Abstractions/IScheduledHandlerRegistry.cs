using System.Collections.Generic;

namespace Core.Scheduling.Abstractions
{
    /// <summary>
    /// 处理器注册表：暴露当前 DI 容器内全部 <see cref="IScheduledHandler"/> 的只读视图。
    /// 运行时与 Quartz Bootstrap 都从这里取 handler。
    /// </summary>
    public interface IScheduledHandlerRegistry
    {
        /// <summary>全部已注册处理器（已按注册顺序去重）。</summary>
        IReadOnlyCollection<IScheduledHandler> GetHandlers();

        /// <summary>按 <see cref="IScheduledHandler.HandlerCode"/> 查找，不存在返回 <see langword="null"/>。</summary>
        IScheduledHandler Find(string handlerCode);
    }
}
