using System;
using System.Linq;
using System.Reflection;

namespace Core.EventBus
{
    /// <summary>
    /// 为消息类型显式指定"消费组 / queue 名"。
    /// </summary>
    /// <remarks>
    /// <para>RabbitMQ subscribe 端按本特性的值确定 queue 名：</para>
    /// <list type="bullet">
    ///   <item><description>同 group → 共享一个 queue → 多实例下消息<b>竞争消费</b>（一条消息只被某一实例处理）</description></item>
    ///   <item><description>不同 group → 各自独立 queue → 广播消费（同一条消息被所有 group 各处理一次）</description></item>
    /// </list>
    /// <para>未标注时回退到入口程序集名（小写），等价于"按服务划分 queue"。</para>
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
        /// 取消息类型上的 <see cref="MessageGroupAttribute"/> 值；
        /// 未标注时回退到 <see cref="Assembly.GetEntryAssembly"/> 的程序集名小写；
        /// EntryAssembly 也不可用时（如测试宿主、library host）回退到消息类所在程序集名小写，
        /// 保证返回值非空，避免下游 broker 用空 queue/group 名造成静默丢失。
        /// </summary>
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
