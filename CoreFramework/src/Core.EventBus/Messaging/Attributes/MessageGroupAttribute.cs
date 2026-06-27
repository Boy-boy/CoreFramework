using System;
using System.Linq;
using System.Reflection;

namespace Core.EventBus
{
    /// <summary>
    /// 为消息类型显式指定消费组 / queue 名。
    /// </summary>
    /// <remarks>
    /// 同 group 共享 queue → 多实例间竞争消费;不同 group 各自独立 queue → 广播消费。
    /// 未标注时回退到入口程序集名小写,等价于"按服务划分 queue"。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageGroupAttribute : Attribute
    {
        /// <summary>消费组名 / queue 名。</summary>
        public virtual string Group { get; }

        public MessageGroupAttribute(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"{nameof(group)} can not be null, empty or white space!");
            }
            Group = group;
        }

        /// <summary>
        /// 取消息类型上的特性值;未标注时回退到入口程序集名小写,再不可用时回退到消息类所在程序集名小写。
        /// </summary>
        /// <remarks>保证返回值非空,避免下游用空 queue/group 名造成静默丢失。</remarks>
        public static string GetGroupOrDefault(Type messageType)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }

            var declared = messageType
                .GetCustomAttributes(true)
                .OfType<MessageGroupAttribute>()
                .FirstOrDefault()
                ?.Group;
            if (!string.IsNullOrEmpty(declared)) return declared;

            var entry = Assembly.GetEntryAssembly()?.GetName().Name;
            if (!string.IsNullOrEmpty(entry)) return entry.ToLowerInvariant();

            return messageType.Assembly.GetName().Name?.ToLowerInvariant() ?? messageType.Namespace ?? "default";
        }
    }
}
