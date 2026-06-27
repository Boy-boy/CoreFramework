using System;
using System.Diagnostics;

namespace Core.EventBus.Diagnostics
{
    /// <summary>
    /// EventBus 的 <see cref="DiagnosticListener"/> 静态适配器;publisher / subscriber 在关键时刻调用本类把状态写入 listener。
    /// </summary>
    /// <remarks>
    /// 每个方法都先做双层 <c>IsEnabled</c> 短路:未启用监听时不分配匿名对象、也不调 Write,几乎零开销。
    /// </remarks>
    public class EventBusDiagnosticListener
    {
        private static readonly DiagnosticListener EventBusDiagnostics = new DiagnosticListener(DiagnosticListenerConstants.DiagnosticListenerName);

        /// <summary>发布消息执行前。</summary>
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

        /// <summary>发布消息执行后。</summary>
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

        /// <summary>发布消息执行错误。</summary>
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

        /// <summary>消费消息执行前。</summary>
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

        /// <summary>消费消息执行后。</summary>
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

        /// <summary>消费消息执行错误。</summary>
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

        /// <summary>消息未被订阅。</summary>
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
