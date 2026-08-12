# CoreFramework（核心框架）

> 面向 .NET 10 的模块化应用开发框架：用模块声明依赖，组合 DDD、持久化、事件总线、调度、权限、配置中心等基础设施，让你**组装应用**而不是手写样板代码。

- [特性亮点](#特性亮点)
- [快速上手](#快速上手)
- [使用方式](#使用方式)
- [能力索引](#能力索引)
- [模块参考表](#模块参考表)
- [配置约定](#配置约定)
- [示例项目](#示例项目)
- [技术规格](#技术规格)

---

## 特性亮点

- **模块化应用组装**：一切能力都是 `CoreModuleBase` 模块，用 `[DependsOn]` 声明依赖，框架按依赖拓扑自动装配、编排生命周期。
- **DDD 底座**：实体 / 聚合根 / 值对象 / 仓储 / 领域服务，聚合根上的**领域事件**随工作单元提交自动进入事件总线。
- **统一事件总线**：进程内 / RabbitMQ / Kafka 三种传输，出箱(Outbox) + 入箱(Inbox) 保证可靠投递与消费幂等。
- **一键持久化**：EF Core + 仓储自动注册 + 工作单元(UoW) 原子提交 + 分表支持。
- **配置中心**：数据库托管的配置源 + 可视化 Dashboard，配置改动热生效。
- **调度**：Quartz / Hangfire / BackgroundService 三种宿主 + Redis 分布式锁，统一 `IScheduledHandler` 契约。
- **周边能力一应俱全**：Redis 缓存、RBAC 权限、第三方 SSO、告警升级、邮件、Excel、S3、翻译、请求管道、CQRS、持久化日志。

---

## 快速上手

### 方式一：从模板创建

本仓库已配置为 `dotnet new` 模板（shortName `CoreTemplate`）：

```bash
dotnet new install <本仓库路径>
dotnet new CoreTemplate -n MyApp
cd MyApp
dotnet run
```

### 方式二：在现有项目接入

添加对所需模块的 `ProjectReference`（如 `Core.Modularity`、`Core.EventBus`），然后写启动模块 + 两行引导：

```csharp
// using Core.Modularity;
// using Core.Modularity.Attribute;
// using Core.EventBus;

[DependsOn(typeof(CoreEventBusModule))]
public class StartupModule : CoreModuleBase
{
    public override void ConfigureServices(ServiceCollectionContext context) { }
    public override void Configure(ApplicationBuilderContext context) { }
}

// Startup.ConfigureServices → services.ConfigureServiceCollection<StartupModule>();
// Startup.Configure          → app.BuildApplicationBuilder();
```

完整引导说明见 [模块化使用指南](docs/guides/modular.md)。

---

## 使用方式

框架提供两种使用方式，底层是同一套能力，区别只在装配方式：

- **模块化使用（推荐）**：用 `CoreModuleBase` + `[DependsOn]` 声明模块依赖，框架按拓扑自动装配并编排生命周期。适合组合多个能力的完整应用。 → [docs/guides/modular.md](docs/guides/modular.md)
- **直接依赖**：跳过模块系统，直接在 `IServiceCollection` 上调用各能力的 `AddXxx` 扩展方法。适合只想用某一项能力的轻量场景。 → [docs/guides/direct-dependency.md](docs/guides/direct-dependency.md)

---

## 能力索引

### 核心

| 指南 | 内容 |
|---|---|
| [modular.md](docs/guides/modular.md) | 模块系统、生命周期、引导启动 —— **入门必读** |
| [eventbus.md](docs/guides/eventbus.md) | 事件总线：发布 / 订阅、RabbitMQ / Kafka、出箱入箱 |
| [efcore.md](docs/guides/efcore.md) | EF Core + 仓储自动注册 + 工作单元 + 领域事件 |
| [dbconfiguration.md](docs/guides/dbconfiguration.md) | 数据库配置中心 + 可视化 Dashboard |
| [redis.md](docs/guides/redis.md) | Redis 缓存：`IRedisCache` + JSON 扩展 + 分布式锁 |
| [scheduling.md](docs/guides/scheduling.md) | 调度：Quartz / Hangfire / BackgroundService 三种宿主 |
| [elasticsearch.md](docs/guides/elasticsearch.md) | Elasticsearch 客户端工厂与仓储 |

### 业务周边

| 指南 | 内容 |
|---|---|
| [permission.md](docs/guides/permission.md) | 权限：RBAC 接口权限控制（`[Permission]` + 中间件） |
| [application.md](docs/guides/application.md) | CQRS 应用层：MediatR + FluentValidation 自动装配 |
| [pipeline.md](docs/guides/pipeline.md) | 请求管道：前置 / 后置处理器 |
| [sso.md](docs/guides/sso.md) | 第三方 OAuth / OIDC 单点登录 |
| [emailclient.md](docs/guides/emailclient.md) | 邮件发送（MailKit + 出箱式存储） |
| [excel.md](docs/guides/excel.md) | Excel 导入导出（EPPlus） |
| [httpclient.md](docs/guides/httpclient.md) | 类型化 HTTP 客户端 + Kerberos 认证 |

### 装配方式

| 指南 | 内容 |
|---|---|
| [direct-dependency.md](docs/guides/direct-dependency.md) | 直接依赖用法与 `AddXxx` 速查表 |

---

## 模块参考表

仓库当前包含 **46 个 `Core.*` 模块**。带 📄 标记的模块自带独立 README，以它为权威文档。

### 模块系统 / 框架核心

| 模块 | 一句话用途 |
|---|---|
| `Core.Modularity` | 模块内核：`CoreModuleBase`、`[DependsOn]`、生命周期编排、拓扑装配 |
| `Core.Framework` | 元包：聚合 DDD、EF Core、EventBus、Modularity、RabbitMQ、Uow 的项目引用 |
| `Core.Application` | CQRS（MediatR）+ FluentValidation 自动装配，`Command` / `Query` 基类型 |
| `Core.AspNetCore` | `ApiResult` 统一返回 + `ApiResultWrapAttribute` 结果包装 |
| `Core.Infrastructure` | 基础工具：`DomainException`、异步定时器、递归模型 |
| `Core.Json` | `System.Text.Json` 助手（camelCase / 缩进预设） |
| `Core.Threading.Tasks` | 并发限流（`ConcurrentManager`） |

### 数据 / 持久化

| 模块 | 一句话用途 |
|---|---|
| `Core.Ddd.Domain` | DDD 原语：实体、聚合根（本地/分布式领域事件）、值对象、仓储、领域服务、软删除 |
| `Core.EntityFrameworkCore` | EF Core 集成：`CoreDbContext`、仓储自动注册、`AddDbContextAndEfRepositories<T>` |
| `Core.EntityFrameworkCore.Sharding` | EF Core 分表（`CoreShardingDbContext`） |
| `Core.Uow` | 工作单元：`IUnitOfWorkManager`、`[UnitOfWork]`、事务边界 |
| `Core.ElasticSearch` | Elasticsearch 客户端工厂 + 仓储基类 |
| `Core.Redis` | Redis 封装：`IRedisCache`（字符串/列表/哈希/集合/Stream + JSON 扩展 + 分布式锁） |
| `Core.Excel` | Excel 导入导出（EPPlus） |

### 消息 / 集成

| 模块 | 一句话用途 |
|---|---|
| `Core.EventBus` 📄 | 事件总线契约 + 进程内总线 + 出箱/入箱抽象（`src/Core.EventBus/README.md`） |
| `Core.EventBus.RabbitMQ` | RabbitMQ 传输实现 |
| `Core.EventBus.Kafka` | Kafka 传输实现 |
| `Core.EventBus.Storage.EfCore` | 出箱 / 入箱 / 死信表的 EF Core 存储 |
| `Core.RabbitMQ` 📄 | RabbitMQ 底层：持久连接（Polly 重试）、消费者管理、发布确认（`src/Core.RabbitMQ/README.md`） |
| `Core.Kafka` 📄 | Kafka 底层：幂等生产者、消费者管理（`src/Core.Kafka/README.md`） |

### 配置

| 模块 | 一句话用途 |
|---|---|
| `Core.Configuration` | 数据库配置源 `IConfigurationProvider` + 变更通知 |
| `Core.Configuration.SqlServer` / `.PostgreSql` / `.MySql` | 配置存储后端 |
| `Core.Configuration.Dashboard` | 配置管理 Dashboard（`MapDbConfigurationDashboard`） |

### Web / 安全

| 模块 | 一句话用途 |
|---|---|
| `Core.Authentication.ThirdParty.Sso` 📄 | 第三方 OAuth/OIDC 单点登录 + 登出通知 Hub（`src/Core.Authentication.ThirdParty.Sso/README.md`） |
| `Core.Permission`（`.PostgreSql`） | RBAC 权限中间件 + 角色权限存储 |
| `Core.PersistentLogging` 📄 | 请求/响应持久化日志：MVC 过滤器 + HttpClient 委托处理器（两处子 README） |

### 调度

| 模块 | 一句话用途 |
|---|---|
| `Core.Scheduling.Abstractions` | 调度契约：`IScheduledHandler`、`ScheduleDescriptor`、执行过滤器 |
| `Core.Scheduling` 📄 | 调度核心：过滤器管道、处理器注册表、状态存储（`src/Core.Scheduling/README.md`） |
| `Core.Scheduling.Background` | 默认 `BackgroundService` 宿主 |
| `Core.Scheduling.Quartz` | Quartz 适配（集群、cron、AdoJobStore） |
| `Core.Scheduling.Hangfire` | Hangfire 适配（RecurringJob、Dashboard） |
| `Core.Scheduling.Redis` | Redis 分布式锁（叠加在 Background 宿主之上） |
| `Core.Scheduling.HealthChecks` | 调度状态 → ASP.NET 健康检查 |

### 其他

| 模块 | 一句话用途 |
|---|---|
| `Core.HttpClient` | 类型化 HTTP 客户端 + Kerberos 认证 |
| `Core.Translate` | 翻译（百度翻译提供方） |
| `Core.Amazon.S3` | S3 客户端工厂 |
| `Core.EmailClient`（`.Mysql` / `.PostgreSql`） | 邮件发送 + 出箱式存储 + 后台发送 |
| `Core.Alert`（`.Redis` / `.Sqlite`） | 告警升级引擎（会话、升级规则、决策） |

---

## 配置约定

框架各模块遵循"模块绑定固定配置节"的约定，配置统一放在 `appsettings.json` 对应节下：

| 配置节 | 绑定选项 | 绑定模块 |
|---|---|---|
| `ElasticSearch` | `ElasticClientFactoryOptions` | `CoreElasticSearchModule` |
| `Redis` | `RedisCacheOptions` | `CoreRedisModule` |
| `EventBus:RabbitMq`（`Broker` 子节点 → `RabbitMqOptions`） | `EventBusRabbitMqOptions` | `CoreEventBusRabbitMqModule` |
| `EventBus:Kafka` | Kafka broker 选项 | `CoreEventBusKafkaModule` |
| `RabbitMq` | `RabbitMqOptions` | `CoreRabbitMqModule` |
| `Amazon:S3` | `AmazonS3Options` | S3 模块 |
| `Translate:BaiDu` | `BaiDuTranslateOptions` | 翻译模块 |
| `Scheduling`（及 `:Background` / `:Quartz` / `:Hangfire` / `:Redis`） | 各调度宿主选项 | `Scheduling*Module` |
| `EmailClient` | `EmailClientOptions` | 邮件模块 |
| 配置中心存储 | 无固定节——由 `AddPostgreSqlConfigure(...)` 等调用处显式传入 `DbConnection` | `Core.Configuration.*` |

---

## 示例项目

仓库 `test/` 下提供可运行的示例（均基于模块化引导）：

| 项目 | 演示 |
|---|---|
| `test/eventBus/PublishApi` | 事件总线**发布端**（RabbitMQ） |
| `test/eventBus/SubscriptionApi` | 事件总线**订阅端**（RabbitMQ） |
| `test/EntityFrameworkCore.Api` | EF Core + 事件总线 + 工作单元组合用法 |
| `test/ThirdPartySso.WebApi` | 第三方 SSO 登录（Minimal API 风格引导） |
| `test/Test` | 单元测试（事件总线、管道、告警、邮件、S3 等） |

---

## 技术规格

- **目标框架**：.NET 10（`net10.0`）
- **版本**：10.0.0
- **许可证**：MIT
- **仓库**：<https://github.com/Boy-boy/CoreFramework>
- **获取方式**：以项目引用（ProjectReference）或 `dotnet new CoreTemplate` 模板接入；暂未发布 NuGet 包。
