using System;
using System.Linq;

namespace Core.EventBus
{
    /// <summary>
    /// 为 handler 指定执行优先级;同一消息存在多个 handler 时按优先级降序执行,数值越大越先执行。
    /// </summary>
    /// <remarks>
    /// 可标注在类(覆盖所有 HandleAsync)或方法(针对单个消息类型覆盖类级别)。未标注时为 0;同优先级之间顺序不保证。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class MessageHandlerPriorityAttribute : Attribute
    {
        /// <summary>优先级;数值越大越先执行。</summary>
        public virtual int Priority { get; }

        public MessageHandlerPriorityAttribute()
        : this(0)
        {
        }

        public MessageHandlerPriorityAttribute(int priority)
        {
            Priority = priority;
        }

        /// <summary>计算 handler 处理某消息类型时的优先级:方法级别 &gt; 类级别 &gt; 0。</summary>
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
            // 按第一个入参类型匹配 HandleAsync,容忍未来签名变体
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
