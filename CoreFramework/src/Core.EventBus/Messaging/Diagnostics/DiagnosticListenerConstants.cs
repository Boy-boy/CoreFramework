namespace Core.EventBus.Messaging.Diagnostics
{
    public static class DiagnosticListenerConstants
    {
        private const string CorePrefix = "Core.EventBus.";

        public const string DiagnosticListenerName = CorePrefix + "DiagnosticListener";

        public const string BeforePublish = CorePrefix + "PublishBefore";
        public const string AfterPublish = CorePrefix + "PublishAfter";
        public const string ErrorPublish = CorePrefix + "PublishError";

        public const string BeforeConsume = CorePrefix + "ConsumeBefore";
        public const string AfterConsume = CorePrefix + "ConsumeAfter";
        public const string ErrorConsume = CorePrefix + "ConsumeError";

        public const string NotSubscribed = CorePrefix + "NotSubscribed";
        
    }
}
