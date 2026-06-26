using System.Collections.Generic;

namespace Core.Scheduling.Abstractions
{
    /// <summary>处理器注册表,暴露 DI 中全部 <see cref="IScheduledHandler"/> 的只读视图。</summary>
    public interface IScheduledHandlerRegistry
    {
        /// <summary>全部已注册 handler(按注册顺序去重)。</summary>
        IReadOnlyCollection<IScheduledHandler> GetHandlers();

        /// <summary>按编码查找,不存在返回 <see langword="null"/>。</summary>
        IScheduledHandler Find(string handlerCode);
    }
}
