using System;
using System.Linq;

namespace Core.EventBus
{
    /// <summary>
    /// 为 handler 指定执行优先级。同一消息存在多个 handler 时，
    /// publisher 按优先级 <b>降序</b> 依次调用，数值越大越先执行。
    /// </summary>
    /// <remarks>
    /// <para><b>可标注位置</b></para>
    /// <list type="bullet">
    ///   <item><description>类：作用于该 handler 类的所有 HandleAsync 重载</description></item>
    ///   <item><description>方法：可针对单个消息类型覆盖类级别优先级</description></item>
    /// </list>
    /// <para>未标注时为 0；同优先级之间执行顺序不保证。</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class MessageHandlerPriorityAttribute : Attribute
    {
        /// <summary>优先级；数值越大越先执行。</summary>
        public virtual int Priority { get; }

        public MessageHandlerPriorityAttribute()
        : this(0)
        {
        }

        public MessageHandlerPriorityAttribute(int priority)
        {
            Priority = priority;
        }

        /// <summary>
        /// 计算 handler 处理某消息类型时的优先级：方法级别 > 类级别 > 0。
        /// </summary>
        public static int GetPriority(Type messageType, Type handlerType)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }
            if (handlerType == null)
            {
                throw new ArgumentNullException(nameof(handlerType));
            }
            // 签名是 (TMessage, CancellationToken),按第一个入参类型匹配;容忍未来的签名变体。
            var handleMethods = handlerType
                .GetMethods()
                .Where(x => x.Name == "HandleAsync");
            foreach (var method in handleMethods)
            {
                var methodParameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
                if (methodParameterTypes.Length < 1 || methodParameterTypes[0] != messageType) continue;
                var methodPriorityAttributes = method.GetCustomAttributes(true).OfType<MessageHandlerPriorityAttribute>().ToList();
                if (methodPriorityAttributes.Any())
                {
                    return methodPriorityAttributes.First().Priority;
                }
            }
            return handlerType.GetCustomAttributes(true).OfType<MessageHandlerPriorityAttribute>().FirstOrDefault()?.Priority ?? 0;
        }
    }
}
