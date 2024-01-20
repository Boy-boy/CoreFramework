## 使用方式
1.自定义实现日志存储介质，且必须继承IHttpClientPersistentLoggingStorageSource
2.在appsetting.json配置文件中添加HttpClientPersistentLogging配置（如：
"HttpClientPersistentLogging": {
    "StorageSources": {
      "Default": [ "Database" ]
    }
  }）
3.services.AddHttpClient("Default").AddPersistentLoggingHttpMessageHandler(configuration)