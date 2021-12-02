using System;
using System.Diagnostics;

namespace Core.EventBus.Messaging.Diagnostics
{
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
                Message = message,
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
                Message = message,
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
                Message = message,
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
                Message = message,
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
                Message = message,
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
                Message = message,
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
                Message = message,
                ExecutionTime = DateTime.UtcNow
            };
            EventBusDiagnostics.Write(DiagnosticListenerConstants.NotSubscribed, result);
        }
    }
}
