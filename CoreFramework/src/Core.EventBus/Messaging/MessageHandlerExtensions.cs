using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Core.EventBus
{
    /// <summary>
    /// handler 程序集扫描工具方法集;被 options 注册与 subscriber 初始化共用。
    /// </summary>
    public static class MessageHandlerExtensions
    {
        /// <summary>扫描程序集,返回所有 <see cref="IMessageHandler"/> 的具体类(非抽象、非接口)。</summary>
        public static IEnumerable<Type> GetHandlerTypes(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
                return Array.Empty<Type>();

            // 过滤接口 / 抽象类:DI 无法解析,扫到只是 noise
            return assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && typeof(IMessageHandler).IsAssignableFrom(t))
                .Select(t => t.AsType())
                .ToList();
        }

        /// <summary>params 重载,适合直接传一/多个 <see cref="Assembly"/> 的调用方(如单测)。</summary>
        public static IEnumerable<Type> GetHandlerTypes(params Assembly[] assemblies)
            => GetHandlerTypes((IEnumerable<Assembly>)assemblies);

        /// <summary>返回 handler 类直接实现的所有 <c>IMessageHandler&lt;T&gt;</c> 泛型接口;一个 handler 可同时处理多种消息。</summary>
        public static IEnumerable<Type> GetBaseHandlerTypes(Type handlerType)
        {
            return handlerType
                .GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .Distinct();
        }
    }
}
