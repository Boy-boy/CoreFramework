using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    /// <summary>
    /// 所有消息(本地事件 + 集成事件)的统一契约。
    /// </summary>
    /// <remarks>
    /// 业务事件继承 <see cref="Message"/> 即可,不要自行实现本接口。
    /// </remarks>
    public interface IMessage
    {
        /// <summary>消息全局唯一 Id;贯穿生产端到消费端,用于链路追踪和幂等去重。</summary>
        Guid Id { get; set; }

        /// <summary>消息生成时刻(UTC),仅供日志排查。</summary>
        DateTime Timestamp { get; set; }

        /// <summary>自由 key-value 元数据,放链路 ID / 租户 / 用户上下文等。</summary>
        IDictionary<string, string> Items { get; }

        /// <summary>合并 items 到 <see cref="Items"/>;同 key 保留已有值不覆盖。</summary>
        void AddItems(IDictionary<string, string> items);

        /// <summary>删除一个元数据项;key 不存在时不报错。</summary>
        void RemoveItem(string itemKey);
    }
}
