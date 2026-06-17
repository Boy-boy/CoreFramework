using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    /// <summary>
    /// EventBus 中所有消息（本地事件 / 集成事件）的统一契约。
    /// </summary>
    /// <remarks>
    /// <para><b>设计要点</b></para>
    /// <list type="bullet">
    ///   <item><description><see cref="Id"/>：贯穿生产端 → outbox → broker → inbox 全链路的唯一标识，
    ///   是 inbox 去重 / 链路追踪的基础。</description></item>
    ///   <item><description><see cref="Timestamp"/>：业务发出时刻的 UTC 时间，便于排查时序。</description></item>
    ///   <item><description><see cref="Items"/>：自由 key-value 槽位，承载链路追踪 ID、租户、用户上下文等
    ///   元数据，不污染业务字段。</description></item>
    /// </list>
    /// 业务事件可直接继承 <see cref="Message"/> 获得默认实现。
    /// </remarks>
    public interface IMessage
    {
        /// <summary>消息全局唯一 Id；同时充当 broker MessageId 和 inbox 主键的一部分。</summary>
        Guid Id { get; set; }

        /// <summary>消息生成时间（UTC）。仅用于日志 / 排查，不参与 outbox 排序。</summary>
        DateTime Timestamp { get; set; }

        /// <summary>消息携带的元数据 key-value 集合。框架不强制 schema，使用方按需约定。</summary>
        IDictionary<string, string> Items { get; }

        /// <summary>
        /// 合并入参 items 到 <see cref="Items"/>（同 key 时保留已存在值，不覆盖）。
        /// </summary>
        void AddItems(IDictionary<string, string> items);

        /// <summary>
        /// 删除一个元数据项；key 不存在时不报错。
        /// </summary>
        void RemoveItem(string itemKey);
    }
}
