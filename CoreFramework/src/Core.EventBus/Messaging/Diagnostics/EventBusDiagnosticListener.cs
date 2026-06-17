using System;
using System.Diagnostics;

namespace Core.EventBus.Diagnostics
{
    /// <summary>
    /// EventBus 的 <see cref="DiagnosticListener"/> 静态适配器。
    /// publisher / subscriber 在关键时刻调用本类方法把状态写入 listener，由 APM / 链路追踪组件消费。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 每个方法都先用 <c>IsEnabled</c> 双层短路检测，确保未启用监听时几乎零开销：
    /// 既不分配匿名对象，也不调 Write。
    /// </para>
    /// <para>
    /// 写入的负载是 <c>{ MessageType, MessageData, ExecutionTime, ... }</c> 匿名对象 —— APM 端按反射读取所需字段。
    /// </para>
    /// </remarks>
    public class EventBusDiagnosticListener
    {
        private static readonly DiagnosticListener EventBusDiagnostics = new DiagnosticListener(DiagnosticListenerConstants.DiagnosticListenerName);

        /// <summary>
        /// 发布消息执行前
        /// </summary>
        /// <param name="message"></param>
        public static void TracingPublishBefore(IMessage message)
        {
            if (!EventBusDiagnostics.IsEnabled() || !EventBusDiagnostics.IsEnabled(DiagnosticListenerConstants.BeforePublish))
                return;
            var result = new
            {
                MessageType = message.GetType(),
                MessageData = message,
                ExecutionTime = DateTime.UtcNow
            };
            EventBusDiagnostics.Write(DiagnosticListenerConstants.BeforePublish, result);
        }

        /// <summary>
        /// 发布消息执行后
        /// </summary>
        /// <param name="message"></param>
        public static void TracingPublishAfter(IMessage message)
        {
            if (!EventBusDiagnostics.IsEnabled() || !EventBusDiagnostics.IsEnabled(DiagnosticListenerConstants.AfterPublish))
                return;

            var result = new
            {
                MessageType = message.GetType(),
                MessageData = message,
                ExecutionTime = DateTime.UtcNow
            };
            EventBusDiagnostics.Write(DiagnosticListenerConstants.AfterPublish, result);
        }

        /// <summary>
        /// 发布消息执行错误
        /// </summary>
        /// <param name="message"></param>
        /// <param name="errorMessage"></param>
        public static void TracingPublishError(IMessage message, string errorMessage)
        {
            if (!EventBusDiagnostics.IsEnabled() || !EventBusDiagnostics.IsEnabled(DiagnosticListenerConstants.ErrorPublish))
                return;

            var result = new
            {
                MessageType = message.GetType(),
                MessageData = message,
                ExecutionTime = DateTime.UtcNow,
                ErrorMessage = errorMessage
            };
            EventBusDiagnostics.Write(DiagnosticListenerConstants.ErrorPublish, result);
        }

        /// <summary>
        /// 订阅消息执行前
        /// </summary>
        /// <param name="message"></param>
        public static void TracingConsumeBefore(IMessage message)
        {
            if (!EventBusDiagnostics.IsEnabled() || !EventBusDiagnostics.IsEnabled(DiagnosticListenerConstants.BeforeConsume))
                return;

            var result = new
            {
                MessageType = message.GetType(),
                MessageData = message,
                ExecutionTime = DateTime.UtcNow
            };
            EventBusDiagnostics.Write(DiagnosticListenerConstants.BeforeConsume, result);
        }

        /// <summary>
        /// 订阅息执行后
        /// </summary>
        /// <param name="message"></param>
        public static void TracingConsumeAfter(IMessage message)
        {
            if (!EventBusDiagnostics.IsEnabled() || !EventBusDiagnostics.IsEnabled(DiagnosticListenerConstants.AfterConsume))
                return;

            var result = new
            {
                MessageType = message.GetType(),
                MessageData = message,
                ExecutionTime = DateTime.UtcNow
            };
            EventBusDiagnostics.Write(DiagnosticListenerConstants.AfterConsume, result);
        }

        /// <summary>
        /// 订阅消息执行错误
        /// </summary>
        /// <param name="message"></param>
        /// <param name="handlerType"></param>
        /// <param name="errorMessage"></param>
        public static void TracingConsumeError(IMessage message, Type handlerType, string errorMessage)
        {
            if (!EventBusDiagnostics.IsEnabled() || !EventBusDiagnostics.IsEnabled(DiagnosticListenerConstants.ErrorConsume))
                return;

            var result = new
            {
                MessageType = message.GetType(),
                MessageData = message,
                HandlerType = handlerType,
                ExecutionTime = DateTime.UtcNow,
                ErrorMessage = errorMessage
            };
            EventBusDiagnostics.Write(DiagnosticListenerConstants.ErrorConsume, result);
        }

        /// <summary>
        /// 消息未被订阅
        /// </summary>
        /// <param name="message"></param>
        public static void TracingNotSubscribed(object message)
        {
            if (!EventBusDiagnostics.IsEnabled() || !EventBusDiagnostics.IsEnabled(DiagnosticListenerConstants.NotSubscribed))
                return;

            var result = new
            {
                MessageType = message.GetType(),
                MessageData = message,
                ExecutionTime = DateTime.UtcNow
            };
            EventBusDiagnostics.Write(DiagnosticListenerConstants.NotSubscribed, result);
        }
    }
}
