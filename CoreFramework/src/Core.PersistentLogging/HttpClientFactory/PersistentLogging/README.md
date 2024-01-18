## 使用方式
1.自定义实现日志存储介质，且必须继承IHttpClientPersistentLoggingStorageSource
2.在appsetting.json配置文件中添加HttpClientPersistentLogging配置（如："HttpClientPersistentLogging":{"StorageSources":["日志存储介质名称"]}）
3.全局注入：AddLoggingHttpMessageHandlerBuilderFilter()，自定义注入：AddPersistentLoggingHttpMessageHandler()，切记，两者只可选其一