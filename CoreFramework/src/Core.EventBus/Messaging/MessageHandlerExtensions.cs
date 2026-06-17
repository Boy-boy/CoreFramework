using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Core.EventBus
{
    /// <summary>
    /// handler 程序集扫描工具方法集。被 <see cref="EventBusOptionsExtensions.Configure"/>
    /// 与 <see cref="Messaging.MessageSubscribeBase.Initialize"/> 共用。
    /// </summary>
    public static class MessageHandlerExtensions
    {
        /// <summary>
        /// 扫描程序集，返回所有 <see cref="IMessageHandler"/> 的具体类（非抽象、非接口）。
        /// </summary>
        public static IEnumerable<Type> GetHandlerTypes(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                return Array.Empty<Type>();

            // 过滤接口 / 抽象类：它们无法被 DI 解析，扫到也只是 noise；
            // 同时排除 IMessageHandler 本身的基接口
            return assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && typeof(IMessageHandler).IsAssignableFrom(t))
                .Select(t => t.AsType())
                .ToList();
        }

        /// <summary>
        /// 返回 handler 类直接实现的所有 <c>IMessageHandler&lt;T&gt;</c> 泛型接口。
        /// 同一 handler 类可同时处理多种消息（实现多个泛型接口），各自展开为一条订阅。
        /// </summary>
        public static IEnumerable<Type> GetBaseHandlerTypes(Type handlerType)
        {
            return handlerType
                .GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .Distinct();
        }
    }
}
