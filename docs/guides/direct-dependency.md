# 直接依赖指南

> 跳过模块系统，直接在 `IServiceCollection` 上调用各能力的 `AddXxx` 扩展方法。适合只想用某一项能力、不引入整套模块装配的轻量场景。

模块化用法与直接依赖**底层是同一套能力**，区别只在装配方式：

- **模块化使用**：通过 `ConfigureServiceCollection<T>()` 让框架收集模块、按 `[DependsOn]` 拓扑装配，自动编排生命周期与配置节绑定。
- **直接依赖**：由你手动调用 `AddXxx` 逐个注册，需要哪个能力就注册哪个。

两种方式可混用：先 `ConfigureServiceCollection<T>()` 走模块装配，再在模块的 `ConfigureServices` 里补注册扩展能力。

## `AddXxx` 速查表

| 能力 | 直接注册 | 包（项目） |
|---|---|---|
| 事件总线 | `services.AddEventBus(options => { ... })` | `Core.EventBus` |
| RabbitMQ 传输 | `AddEventBus` 后 `options.AddRabbitMq(cfg)` | `Core.EventBus.RabbitMQ` |
| Kafka 传输 | `AddEventBus` 后 `options.AddKafka(cfg)` | `Core.EventBus.Kafka` |
| EF Core + 仓储 | `services.AddDbContextAndEfRepositories<TDbContext>(...)` | `Core.EntityFrameworkCore` |
| 工作单元 | `services.AddUnitOfWork()` | `Core.Uow` |
| 请求管道 | `services.AddPipeline(assemblies)` | `Core.Pipeline` |
| CQRS / 校验 | `services.AddApplication()` | `Core.Application` |
| Redis 缓存 | `services.AddRedisCache(options => ...)` | `Core.Redis` |
| Elasticsearch | `services.AddElasticClientFactory(...)` | `Core.ElasticSearch` |
| 配置中心 | `services.AddDbConfiguration(options => ...)` | `Core.Configuration` |
| 权限 | `services.AddPermission(options => ...)` | `Core.Permission` |
| 邮件 | `services.AddEmailClient(options => ...)` | `Core.EmailClient` |
| 告警 | `services.AddAlertEscalation(options => ...)` | `Core.Alert` |
| HTTP 客户端 | `services.AddBaseHttpClient()` | `Core.HttpClient` |
| 亚马逊 S3 | `services.AddAmazonS3(options => ...)` | `Core.Amazon.S3` |
| 翻译 | `services.AddBaiDuTranslate(options => ...)` | `Core.Translate` |
| 调度 | `services.AddSchedulingBackground/Quartz/Hangfire(...)` | `Core.Scheduling.*` |

> 各能力的完整用法见对应的[能力索引](../README.md#能力索引)指南，或带 📄 模块的独立 README（`src/Core.EventBus/README.md`、`src/Core.Scheduling/README.md` 等）。
