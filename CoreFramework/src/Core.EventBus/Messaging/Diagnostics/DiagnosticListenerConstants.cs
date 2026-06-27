namespace Core.EventBus.Diagnostics
{
    /// <summary>
    /// <see cref="System.Diagnostics.DiagnosticListener"/> 的事件名常量集合;用于 APM / 链路追踪订阅 EventBus 内部信号。
    /// </summary>
    /// <remarks>
    /// 统一以 <c>Core.EventBus.</c> 为前缀,避免与其它库的 listener 命名冲突。
    /// </remarks>
    public static class DiagnosticListenerConstants
    {
        private const string CorePrefix = "Core.EventBus.";

        /// <summary>EventBus 总 listener 名。</summary>
        public const string DiagnosticListenerName = CorePrefix + "DiagnosticListener";

        /// <summary>发布前事件名。</summary>
        public const string BeforePublish = CorePrefix + "PublishBefore";
        /// <summary>发布后事件名。</summary>
        public const string AfterPublish = CorePrefix + "PublishAfter";
        /// <summary>发布失败事件名。</summary>
        public const string ErrorPublish = CorePrefix + "PublishError";

        /// <summary>消费前事件名。</summary>
        public const string BeforeConsume = CorePrefix + "ConsumeBefore";
        /// <summary>消费后事件名。</summary>
        public const string AfterConsume = CorePrefix + "ConsumeAfter";
        /// <summary>消费失败事件名。</summary>
        public const string ErrorConsume = CorePrefix + "ConsumeError";

        /// <summary>收到未订阅消息事件名(典型为配置遗漏 / 版本错位)。</summary>
        public const string NotSubscribed = CorePrefix + "NotSubscribed";

    }
}
