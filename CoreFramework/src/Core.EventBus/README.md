# Core.EventBus

> 统一事件总线 —— 同一份业务代码，本地内调 / RabbitMQ / Kafka 切换不动 handler。事务一致性（outbox）与幂等消费（inbox）即插即用。

业务侧只见三个核心契约：`IMessage`（事件）、`IMessageHandler<T>`（处理器）、`IIntegrationPublisher` / `ILocalPublisher`（发布者）。换 broker 只改启动注册一行，handler 文件零修改。

---

## 目录

- [包结构](#包结构)
- [30 秒上手](#30-秒上手)
- [核心概念](#核心概念)
- [注册方式：模块化 vs ServiceCollection](#注册方式模块化-vs-servicecollection)
- [场景 1：本地（进程内）事件](#场景-1本地进程内事件)
- [场景 2：跨服务集成事件](#场景-2跨服务集成事件)
  - [RabbitMQ](#rabbitmq)
  - [Kafka](#kafka)
- [场景 3：事务一致性（Outbox 模式）](#场景-3事务一致性outbox-模式)
- [场景 4：幂等消费（Inbox 模式）](#场景-4幂等消费inbox-模式)
- [Handler 编写契约](#handler-编写契约)
- [Attributes 速查](#attributes-速查)
- [配置详解](#配置详解)
- [appsettings.json 完整模板](#appsettingsjson-完整模板)
- [常见坑 / FAQ](#常见坑--faq)
- [架构内幕](#架构内幕)

---

## 包结构

| 包 | 职责 | 什么时候用 |
|---|---|---|
| `Core.EventBus` | **核心契约 + 进程内事件总线**：`IMessage` / `IMessageHandler<T>` / `IIntegrationPublisher` / `ILocalPublisher` / outbox/inbox 抽象 | 永远引入 |
| `Core.EventBus`（命名空间 `Local`） | 默认的进程内 publisher / subscriber 实现 | 只想要进程内 pub/sub（无 broker） |
| `Core.EventBus.RabbitMQ` | RabbitMQ broker 实现 | 跨进程集成事件，走 AMQP broker |
| `Core.EventBus.Kafka` | Kafka broker 实现 | 跨进程集成事件，走 Kafka（高吞吐 / 流处理） |
| `Core.EventBus.Storage.EfCore` | **outbox + inbox** 的 EF Core 存储实现 | 需要事务一致性发布 + 幂等消费（强烈推荐） |
| `Core.RabbitMQ` | RabbitMQ 底层连接 / 消费者基础设施 | 通常作为 `Core.EventBus.RabbitMQ` 的传递依赖自动引入 |
| `Core.Kafka` | Kafka 底层 producer / consumer 基础设施 | 通常作为 `Core.EventBus.Kafka` 的传递依赖自动引入 |

---

## 30 秒上手

**1. 定义事件 + 写 handler：**

```csharp
[MessageName("order.created")]                  // broker 路由名 / Kafka topic
[MessageGroup("order-service")]                 // 消费组 / RabbitMQ queue / Kafka group.id
public class OrderCreatedEvent : Message
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}

public class OrderCreatedHandler : IMessageHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent message, CancellationToken ct = default)
    {
        // ← 业务逻辑
        return Task.CompletedTask;
    }
}
```

**2. 启动注册（非模块化）：**

```csharp
services.AddEventBus(options =>
{
    options.AddConsumers(typeof(OrderCreatedHandler).Assembly);
    options.AddLocalMq();                                                 // 本地事件
    options.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));   // 或 .AddKafka(...)
});
```

**3. 业务侧发布：**

```csharp
public class OrderController(IIntegrationPublisher publisher) : ControllerBase
{
    [HttpPost]
    public Task Post() => publisher.PublishAsync(new OrderCreatedEvent { ... });
}
```

完。Handler 会被自动扫描注册、在消息到达时由框架解析 DI scope 调用。

> 注意方法名必须是 `HandleAsync`：反射定位依赖此名称，不要拼错。

---

## 核心概念

### `IMessage` —— 事件契约

所有事件继承自 `Message`（默认实现）即可获得 `Id` / `Timestamp` / `Items` 元数据容器。

```csharp
public interface IMessage
{
    Guid Id { get; set; }                            // 全链路 ID：贯穿 producer → outbox → broker → inbox
    DateTime Timestamp { get; set; }                 // UTC 生成时间
    IDictionary<string, string> Items { get; }       // 自由元数据（链路追踪、租户、用户上下文）
}
```

> 自定义事件**不要**自己实现 `IMessage`，继承 `Message` 即可。

### `IMessageHandler<T>` —— 处理器契约

```csharp
public interface IMessageHandler<in TMessage> : IMessageHandler where TMessage : class, IMessage
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
```

- 一个 handler 类可以同时实现多个 `IMessageHandler<T>`，处理不同事件。
- handler 的 scope 归属按发布路径区分：
  - **broker 消费**（Kafka / RabbitMQ）：`InboxAwareMessageHandlerInvoker` 在**每条消息**新开 scope + 新 UoW —— 注入的 Scoped 服务（DbContext / Repository）属于这个新 scope。
  - **本地事件**（`ILocalPublisher`）：`LocalMessageHandlerInvoker` **复用调用方 scope** —— handler 拿到的 DbContext / Repository 与发布者业务共享，自动加入外层 UoW（详见"场景 1"）。

### Publisher 接口（命名简明，按发布目标二选一）

| 接口 | 用途 | 实现 |
|---|---|---|
| `ILocalPublisher` | 进程内派发 | `LocalMessagePublisher`（同步调所有 handler） |
| `IIntegrationPublisher` | 跨服务派发 | `RabbitMqMessagePublisher` / `KafkaMessagePublisher` |

业务代码统一注入接口（不要注入实现类），换 broker 时业务代码零修改。

---

## 注册方式：模块化 vs ServiceCollection

仓库使用 `Core.Modularity` 模块体系；EventBus 同时支持两种注册方式，**结果等价**。

### A. 模块化场景（推荐）

```csharp
[DependsOn(
    typeof(CoreEventBusModule),               // 必须
    typeof(CoreEventBusLocalModule),          // 本地事件（可选）
    typeof(CoreEventBusRabbitMqModule),       // 或 CoreEventBusKafkaModule
    typeof(CoreEventBusEfCoreStorageModule)   // outbox + inbox（强烈推荐）
)]
public class MyServiceModule : CoreModuleBase
{
    public override void ConfigureServices(ServiceCollectionContext context)
    {
        context.Services.Configure<EventBusOptions>(options =>
        {
            options.AddConsumers(typeof(OrderCreatedHandler).Assembly);
            options.AddEfCoreEventBusStorage<MyDbContext>();
        });
    }
}
```

> 配置由 broker 模块自动从 `appsettings.json` 的 `EventBus:RabbitMq` / `EventBus:Kafka` 节点读取。

### B. 直接 ServiceCollection 扩展

```csharp
services.AddEventBus(options =>
{
    options.AddConsumers(typeof(Startup).Assembly);     // 程序集扫描 handler

    options.AddLocalMq();                                // 本地事件总线
    options.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));
    // 或 options.AddKafka(Configuration.GetSection("EventBus:Kafka"));

    options.AddEfCoreEventBusStorage<MyDbContext>(
        configureOutbox: o => { o.BatchSize = 200; o.MaxRetries = 5; },
        configureInbox:  i => { i.RetentionDays = 30; });
});
```

> 两种方式都会注册同一套 `IHostedService`：`EventBusBackgroundService`（启动期扫订阅）+ `OutboxDispatcher` + `InboxCleanupService`（若启用存储）。

---

## 场景 1：本地（进程内）事件

**适用：** 同进程内解耦（DDD 中的"领域事件"）；不跨服务、不需要持久化。

```csharp
public class WelcomeEmailHandler : IMessageHandler<CustomerRegisteredEvent>
{
    private readonly IEmailService _email;
    public WelcomeEmailHandler(IEmailService email) => _email = email;

    public Task HandleAsync(CustomerRegisteredEvent msg, CancellationToken ct = default)
        => _email.SendWelcomeAsync(msg.CustomerId, ct);
}

// 业务侧
public class CustomerController(ILocalPublisher localPublisher) : ControllerBase
{
    [HttpPost]
    public async Task Register()
    {
        // ... 业务逻辑
        await localPublisher.PublishAsync(new CustomerRegisteredEvent { CustomerId = "C-1" });
    }
}
```

**语义要点：**
- 所有匹配的 handler 在 **PublishAsync 同步执行**（按优先级降序）。
- 单个 handler 抛异常**不会阻断其他 handler**：异常被收集，循环结束后聚合 `AggregateException` 上抛。
- **不走 outbox**：本地事件没有"broker 不可达"问题，直接走 DI scope。
- **事务边界（重要）**：本地路径走 `LocalMessageHandlerInvoker`（Scoped，**不开新 scope、不开新 UoW、不查 inbox**），handler 与发布者共享调用方 scope。事务归属由调用上下文决定：
  - **发布者在外层 UoW 内**（常见 — 业务在 UoW 里 publish + commit）：handler 内拿到的 `DbContext` 自动加入外层 UoW，handler 与发布者**共享同一事务** — 发布者回滚会撤销 handler 数据；handler 失败聚合上抛 → 外层 `UoW.CommitAsync` 失败 → 业务回滚。
  - **发布者在 UoW 外**（罕见 — 测试 / hosted service 直发）：handler 内 `DbContext` 走自己的 `SaveChanges` 即时落库，每个 handler 独立。
- 本地路径**不查 inbox**：本地事件不存在 broker 重投，无需去重；inbox 表只对 broker 消费侧有意义。
- 想要 best-effort 语义（handler 失败不影响发布者业务），请在 handler 内 try-catch 吞掉异常。
- 跨服务长事务 / 补偿 / 超时管理才需要 Saga，单机本地事件这条路径上同事务一致性已具备。

---

## 场景 2：跨服务集成事件

### RabbitMQ

**注册：**

```csharp
options.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));
```

**配置（appsettings.json）：**

```json
{
  "EventBus": {
    "RabbitMq": {
      "ExchangeName": "event_bus_default_routing",
      "ChannelPoolSize": 8,
      "FailureBehavior": "RequeueOnce",
      "DeadLetterExchange": "event_bus_dlx",
      "DeadLetterRoutingKey": null,
      "Connection": {
        "HostName": "rabbit-1;rabbit-2;rabbit-3",
        "Port": 5672,
        "UserName": "guest",
        "Password": "guest",
        "VirtualHost": "/"
      }
    }
  }
}
```

**路由模型：**
- 所有事件共享一个 **direct exchange**（`ExchangeName`）。
- `[MessageName]` → routing key（事件类的对外名）。
- `[MessageGroup]` → queue 名（消费组：同 group 多实例**竞争消费**，不同 group **广播**）。

**发布端容错（publisher confirms + channel 池）：**
- publisher 注册为 **Scoped**（详见架构内幕一节）；所有 scope 共享同一个 Singleton `RabbitMqPublishChannelPool`（大小由 `ChannelPoolSize` 控制，默认 8）。每条 channel 在首次创建时一次性完成 `ExchangeDeclare` + `ConfirmSelect` + `BasicReturn` 监听挂载，后续 publish 复用同一 channel 只走纯 `BasicPublish` + `WaitForConfirms`。
- 每条消息都标 `mandatory: true` + `DeliveryMode: 2`，落 broker 磁盘且无路由时立即 return。
- `WaitForConfirmsOrThrow(5s)` 把"被退回 / nack / 等待超时"统一转成 `RabbitMqPublishFailedException`（派生类：`RabbitMqPublishReturnedException` / `RabbitMqPublishUnconfirmedException`），不会再被错误地当作"发布成功"。
- 仅对**连接级**瞬时错误（`BrokerUnreachableException` / `SocketException`）做 Polly 3 次线性退避（1s/2s/3s）；其他失败原样冒到上游（业务侧捕获或被 outbox dispatcher 进入 MarkFailed 退避循环）。

**消费端失败行为（`FailureBehavior`）：**

| 值 | 语义 | 适用 |
|---|---|---|
| `RequeueOnce`（默认） | 首次失败 `nack(requeue=true)`，二次失败 `nack(requeue=false)`。配合 inbox 去重达成"最终一致"，同时给 poison message 一个截断口避免无限循环 | 大多数业务（与 outbox + inbox 配合最紧密） |
| `NackNoRequeue` | 失败立即 `nack(requeue=false)`；进 DLX 或丢弃 | 可观测但接受丢失 |
| `AlwaysAck` | 失败也 ack —— **完全依赖**上层 inbox 兜底，否则消息静默丢失 | 与旧版本兼容 |

> **DLX 提醒：** `RequeueOnce`/`NackNoRequeue` 在非重投路径会丢消息。若没配 `DeadLetterExchange`，订阅器启动时会一次性 `LogWarning` 提醒；二次失败的消息会被 broker 直接丢弃，运维无法回溯。配 DLX 时框架在 queue 声明时自动写入 `x-dead-letter-exchange`（以及可选 `x-dead-letter-routing-key`），但 DLX → DLQ 的绑定需要运维自行完成（业务语义太多样，框架不代办）。

**单 handler 失败处理：** 同一 routing key 下的多个 handler 仍然按优先级依次执行；某个 handler 抛异常**不阻断**后续 handler，但循环结束会聚合上抛 `AggregateException` —— 让 broker 层按 `FailureBehavior` 决定 ack/nack。与本地事件的"静默 catch"不一样：本地 publisher 不可能丢失消息，broker 路径才需要把失败信号还原回去。

### Kafka

**注册：**

```csharp
options.AddKafka(Configuration.GetSection("EventBus:Kafka"));
```

**配置（appsettings.json）：**

```json
{
  "EventBus": {
    "Kafka": {
      "TopicPrefix": "prod.",
      "DeclareTopicsOnSubscribe": false,
      "DefaultPartitionCount": 3,
      "DefaultReplicationFactor": 2,
      "FailureBackoff": "00:00:05",
      "MaxConsecutiveFailures": 5,
      "Connection": {
        "BootstrapServers": "kafka-1:9092,kafka-2:9092,kafka-3:9092",
        "SecurityProtocol": "SaslSsl",
        "SaslMechanism": "ScramSha512",
        "SaslUsername": "app",
        "SaslPassword": "***"
      }
    }
  }
}
```

**路由模型：**
- `[MessageName]` → topic 名（可选 `TopicPrefix` 前缀，例如多环境共享 broker 时区分）。
- `[MessageGroup]` → `group.id`（同 group → partition 分给多实例**竞争消费**；不同 group → **广播**）。
- 用 `message.Id` 作为 partition key，保证同 Id 消息有序落同一 partition。

**关键差异 vs RabbitMQ（顺着 Kafka 语义，不是"翻译"）：**

| 维度 | RabbitMQ 实现 | Kafka 实现 | 为什么不同 |
|---|---|---|---|
| Producer 重试 | Polly 3 次线性退避（连接级异常）+ publisher confirms | 客户端 `MessageSendMaxRetries=5` + `EnableIdempotence=true` + `Acks=All` | Kafka 客户端原生重试 + broker 侧 producer id 去重；RabbitMQ 用 confirms + mandatory 把"未达成路由"变成可观测异常 |
| 发布失败信号 | `RabbitMqPublishFailedException`（返回 / nack / 超时三种派生异常） | `ProduceAsync` 报错或 `PersistenceStatus.NotPersisted` 直接抛 | 让 outbox dispatcher 能准确 `MarkFailed`，不会被错误地标"已成功" |
| 单条失败处理 | `FailureBehavior` 控制（`RequeueOnce` 默认：首次重投，二次进 DLX/丢弃；可选 `NackNoRequeue` / `AlwaysAck`） | `JsonException` → 立即 commit 跳过；handler 异常 → `Seek` 重投 + `FailureBackoff` 退避，达 `MaxConsecutiveFailures`（默认 5）后 commit 跳过 | 与 RabbitMQ 对偶："失败 → broker 重投，有上限"。Kafka 端 broker 不变，由 consumer 自己拨回 cursor + 计数；deserialization 算永久错误立即跳过；handler 异常给瞬时故障留 N 次重试余地 |
| 消费线程 | broker 推 + `AsyncEventingBasicConsumer` | `Consume()` poll 循环（`LongRunning` Task） | Confluent.Kafka 是 poll-based；订阅变更挂 `_subscriptionDirty` 位由 poll 线程同源应用，避免跨线程操作 librdkafka |
| 投递保证 | `mandatory=true` + `DeliveryMode=2` + ConfirmSelect | `Acks=All` + `EnableIdempotence=true` + ISR | 等价语义，但 Kafka 要求 topic 复制因子 ≥ 2 |

> 想要**完全一致的业务语义**（消息不丢 + 幂等）：**两种 broker 都强烈建议叠加 outbox + inbox**（见下文）。

---

## 场景 3：事务一致性（Outbox 模式）

**问题：** 业务侧"写入 DB + 发出事件"两步分离时，broker 不可达 / 进程崩溃会导致状态不一致：
- 先写 DB 后发事件 → 事件可能丢；
- 先发事件后写 DB → 事件可能误发（DB 回滚但 broker 已收）。

**Outbox 解法：** 在业务事务内**把事件写到 outbox 表**（与业务行共事务），后台 `OutboxDispatcher` 异步投递 broker。

### 启用

```csharp
options.AddEfCoreEventBusStorage<MyDbContext>(
    configureOutbox: o =>
    {
        o.BatchSize = 200;
        o.PollInterval = TimeSpan.FromSeconds(2);
        o.MaxRetries = 8;
    });
```

**前置条件：**
1. 注册了 UoW（`CoreUnitOfWorkModule` 自动完成）。
2. 业务 `DbContext` 在 `OnModelCreating` 中调用：
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       base.OnModelCreating(modelBuilder);
       modelBuilder.AddEventBusStorage();   // outbox + inbox + dead_letter 三张表
   }
   ```
3. 已注册任一 broker 模块（提供 `IOutboxRawSender`）。

### 使用（业务侧零感知）

```csharp
public class OrderController(
    IIntegrationPublisher publisher,
    IUnitOfWorkManager uowManager) : ControllerBase
{
    [HttpPost]
    public async Task Post()
    {
        await using var uow = uowManager.Begin(new UnitOfWorkOptions(isTransactional: true));

        // 业务写库 + 发布事件，全部在同一事务里：
        // PublishAsync 检测到 outbox 上下文 → 写 outbox 表（不直发 broker）
        await publisher.PublishAsync(new OrderCreatedEvent { ... });

        await uow.CommitAsync();   // 一次 commit：业务行 + outbox 行原子落库
    }
}
```

### 路由判定（`IntegrationMessagePublisherBase` 自动接管）

1. 当前是否在 UoW 中？
   - **是** → `IOutboxStorage.StoreMessageAsync`（持久化到 outbox 表）
   - **否** → 直发 broker（best-effort）

> 没在 UoW 里的 `PublishAsync` 也能工作，但**没有事务保证**。生产环境业务路径建议总在 UoW 内发布。

### 死信表

单条消息失败次数超过 `MaxRetries`（默认 8 次，配合指数退避总共约 ~20 分钟）会被移入死信表，停止重试。需要人工排查。

---

## 场景 4：幂等消费（Inbox 模式）

**问题：** broker 至少一次投递、网络重试、消费方崩溃都可能导致一条消息**被同一个 handler 处理多次**。

**Inbox 解法：** 消费端用 `(messageId, handlerType)` 作为 inbox 表主键。`InboxAwareMessageHandlerInvoker` 在调用 handler 前查 inbox：
- 命中 → 跳过（已处理过）；
- 未命中 → 在事务内"调 handler + 写 inbox"原子落库。

### 启用

调 `AddEfCoreEventBusStorage<TDbContext>(...)` **同时启用 outbox 和 inbox**。无须额外配置。

```csharp
options.AddEfCoreEventBusStorage<MyDbContext>(
    configureInbox: i =>
    {
        i.RetentionDays = 14;
        i.CleanupInterval = TimeSpan.FromHours(1);
    });
```

### 使用（业务侧零感知）

handler 写法**完全不变**，框架自动包 UoW + inbox 检查：

```csharp
public class ChargeCustomerHandler : IMessageHandler<OrderCreatedEvent>
{
    private readonly MyDbContext _db;
    public ChargeCustomerHandler(MyDbContext db) => _db = db;

    public async Task HandleAsync(OrderCreatedEvent msg, CancellationToken ct)
    {
        _db.Charges.Add(new Charge { OrderId = msg.OrderId, Amount = msg.Amount });
        // 不需要手动 SaveChanges / 不需要查 inbox：
        // 此处是 broker 路径 —— InboxAwareMessageHandlerInvoker 自己开 UoW + 查 inbox + 调 handler + Commit,
        // handler 写入跟 inbox 行同事务落库
    }
}
```

> ⚠️ `RetentionDays` 必须大于 broker 端可能的最大重投延迟，否则边界上会出现"清理后又收到重投 → 重复处理"的真空。

---

## Handler 编写契约

### 单个 handler 处理多个事件

```csharp
public class AuditHandler :
    IMessageHandler<OrderCreatedEvent>,
    IMessageHandler<OrderCancelledEvent>
{
    public Task HandleAsync(OrderCreatedEvent m, CancellationToken ct) => Audit("created", m.OrderId);
    public Task HandleAsync(OrderCancelledEvent m, CancellationToken ct) => Audit("cancelled", m.OrderId);
}
```

### 多个 handler 处理同一事件 + 优先级

```csharp
[MessageHandlerPriority(10)]
public class FraudCheckHandler : IMessageHandler<OrderCreatedEvent> { ... }   // 先执行

[MessageHandlerPriority(1)]
public class SendEmailHandler : IMessageHandler<OrderCreatedEvent> { ... }    // 后执行
```

数值越大越先执行；未标注为 0。同优先级之间**顺序不保证**。

### Handler 中可以做什么

| 推荐 | 不推荐 |
|---|---|
| 注入 Scoped 服务（`DbContext` / Repository） | 持有跨调用可变状态 — broker 路径每条新 scope；本地路径与调用方共享 scope，长期持有也会被回收 |
| 写库 / 调外部 API / 发新事件 | 在 handler 里手动开 `BeginTransaction`（外层 UoW 已经管理） |
| 抛业务异常让 inbox 重试（broker）/ 让外层 UoW 回滚（本地） | 在 handler 内吞掉所有异常（broker 端消息会被当作成功；本地端发布者业务无法回滚）|

---

## Attributes 速查

| Attribute | 作用 | 默认 |
|---|---|---|
| `[MessageName("xxx")]` | 对外名 / RabbitMQ routing key / Kafka topic | `Type.FullName` |
| `[MessageGroup("xxx")]` | 消费组（queue / `group.id`） | 入口程序集名（小写） |
| `[MessageHandlerPriority(N)]` | handler 执行优先级（降序） | 0 |

**经验法则：**
- **对外发布**的事件**总是标 `[MessageName]`** —— CLR 重命名时不影响 broker 端契约。
- **多个服务订阅同一事件**时**总是标 `[MessageGroup]`** —— 避免不同服务无意中落到同 queue 竞争消费。

---

## 配置详解

### Outbox（`OutboxOptions`）

| 属性 | 默认 | 说明 |
|---|---|---|
| `PollInterval` | 2 秒 | dispatcher 空闲轮询间隔；有消息时立刻拉下一批 |
| `BatchSize` | 100 | 单轮拉取上限；越大吞吐越高，但单事务失败回滚代价越大 |
| `MaxRetries` | 8 | 超过后进死信表 |
| `InitialBackoff` | 5 秒 | 指数退避起步 |
| `MaxBackoff` | 10 分钟 | 退避上限 |
| `AutoInitialize` | true | 启动建表；用 EF Migrations 时关掉 |

### Inbox（`InboxOptions`）

| 属性 | 默认 | 说明 |
|---|---|---|
| `RetentionDays` | 14 | 必须 > broker 最大重投延迟 |
| `CleanupInterval` | 1 小时 | 清理频率 |
| `AutoInitialize` | true | 启动建表 |

### RabbitMQ（`EventBusRabbitMqOptions`）

| 属性 | 默认 | 说明 |
|---|---|---|
| `ExchangeName` | `event_bus_default_routing` | 共享 direct exchange 名 |
| `ChannelPoolSize` | 8 | publisher channel 池上限，即同时持有 channel 的线程数；池外并发请求排队等待 |
| `FailureBehavior` | `RequeueOnce` | handler 失败时的 ack/nack 策略：`AlwaysAck` / `NackNoRequeue` / `RequeueOnce`（见上文）|
| `DeadLetterExchange` | 空 | 设置后 queue 声明会带上 `x-dead-letter-exchange`；启用 DLX 但未设此项 + 失败策略会丢消息 → 订阅器启动时一次性 LogWarning |
| `DeadLetterRoutingKey` | 空 | 仅 `DeadLetterExchange` 设置时生效；留空复用原 routing key（适用 direct DLX） |
| `Connection.HostName` | — | 单机或 `host1;host2;host3` 集群 |
| `Connection.Port` / `UserName` / `Password` / `VirtualHost` | — | AMQP 标准参数 |

### Kafka（`EventBusKafkaOptions`）

| 属性 | 默认 | 说明 |
|---|---|---|
| `TopicPrefix` | (空) | 全局 topic 前缀；多环境共享 broker 时区分 |
| `DeclareTopicsOnSubscribe` | false | 订阅时用 AdminClient 显式建 topic；false 即信任 broker 的 auto-create |
| `DefaultPartitionCount` | 3 | 显式建 topic 时的 partition 数 |
| `DefaultReplicationFactor` | 1 | 显式建 topic 时的副本数（生产建议 ≥ 2） |
| `FailureBackoff` | 5 秒 | handler 抛异常时 PollLoop Seek 回 offset 重投前的退避，避免热循环。桥接到底层 `Core.Kafka.KafkaOptions.FailureBackoff` |
| `MaxConsecutiveFailures` | 5 | 同条 offset 连续失败上限；命中后 commit 跳过该消息，避免 poison message 永久阻塞 partition。设 `0` 关闭。桥接到 `Core.Kafka.KafkaOptions.MaxConsecutiveFailures` |
| `Connection.BootstrapServers` | — | `host1:9092,host2:9092` 逗号分隔 |
| `Connection.SecurityProtocol` | — | `Plaintext` / `Ssl` / `SaslPlaintext` / `SaslSsl` |
| `Connection.SaslMechanism` | — | `Plain` / `ScramSha256` / `ScramSha512` |
| `Connection.SaslUsername` / `SaslPassword` | — | SASL 凭据 |

---

## appsettings.json 完整模板

### 仅 RabbitMQ + outbox/inbox

```json
{
  "EventBus": {
    "RabbitMq": {
      "ExchangeName": "myapp.events",
      "ChannelPoolSize": 8,
      "FailureBehavior": "RequeueOnce",
      "DeadLetterExchange": "myapp.events.dlx",
      "Connection": {
        "HostName": "rabbit:5672",
        "Port": 5672,
        "UserName": "app",
        "Password": "***",
        "VirtualHost": "/"
      }
    },
    "Outbox": {
      "PollInterval": "00:00:02",
      "BatchSize": 200,
      "MaxRetries": 8
    },
    "Inbox": {
      "RetentionDays": 14,
      "CleanupInterval": "01:00:00"
    }
  }
}
```

```csharp
services.AddEventBus(opts =>
{
    opts.AddConsumers(typeof(Startup).Assembly);
    opts.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));
    opts.AddEfCoreEventBusStorage<MyDbContext>(
        Configuration.GetSection("EventBus:Outbox"),
        Configuration.GetSection("EventBus:Inbox"));
});
```

### 仅 Kafka + outbox/inbox

```json
{
  "EventBus": {
    "Kafka": {
      "TopicPrefix": "prod.",
      "DefaultReplicationFactor": 3,
      "Connection": {
        "BootstrapServers": "kafka-1:9092,kafka-2:9092,kafka-3:9092",
        "SecurityProtocol": "SaslSsl",
        "SaslMechanism": "ScramSha512",
        "SaslUsername": "app",
        "SaslPassword": "***"
      }
    },
    "Outbox": { "BatchSize": 200 },
    "Inbox": { "RetentionDays": 14 }
  }
}
```

```csharp
services.AddEventBus(opts =>
{
    opts.AddConsumers(typeof(Startup).Assembly);
    opts.AddKafka(Configuration.GetSection("EventBus:Kafka"));
    opts.AddEfCoreEventBusStorage<MyDbContext>(
        Configuration.GetSection("EventBus:Outbox"),
        Configuration.GetSection("EventBus:Inbox"));
});
```

### 本地事件 + 跨服务集成事件并存

```csharp
services.AddEventBus(opts =>
{
    opts.AddConsumers(typeof(Startup).Assembly);
    opts.AddLocalMq();                                                    // 本地事件
    opts.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));      // 集成事件
    opts.AddEfCoreEventBusStorage<MyDbContext>();
});

// 业务里同时注入两个 publisher，按场景选
public MyController(ILocalPublisher local, IIntegrationPublisher integration) { ... }
```

---

## 常见坑 / FAQ

### Q: handler 不触发？

按这个顺序排查：

1. **handler 程序集没注册：** 检查 `options.AddConsumers(typeof(MyHandler).Assembly)` 是否调用。
2. **handler 不是 public 或是抽象类：** 框架扫描只取具体的 `public` 类。
3. **方法名拼错：** 接口定义就是 `HandleAsync`，方法名错的话反射调用不到。
4. **broker 没注册：** 检查启动日志是否有 `"EventBus 启动时未发现任何 handler 程序集"` 或 `"No subscription for ... event"` 警告。
5. **跨服务发布**没看到下游消费：检查双方 `[MessageName]` 是否完全一致；检查双方 `[MessageGroup]` 是否在同一 group（同 group 竞争，不同 group 才广播）。

### Q: 同一条消息被处理两次？

**正常的**。broker 在以下情况会重投：
- 消费侧处理过程中崩溃（未 ack）；
- broker 重启 / 网络抖动；
- 集群 rebalance（Kafka）。

**这就是为什么必须用 inbox。** `AddEfCoreEventBusStorage<TDbContext>()` 启用后框架自动按 `(messageId, handlerType)` 去重。

### Q: 没用 UoW 包 `PublishAsync`，事件直接发了 broker，业务回滚了怎么办？

事件已经在 broker 上，下游可能已消费。这是**业务侧主动放弃了事务一致性保证**。生产代码请：
- 关键事件**总在 UoW 内发布**；
- 用 outbox 兜底（`PublishAsync` 自动检测 UoW 上下文写 outbox）。

### Q: outbox 表越堆越多？

- 是否启动了 `OutboxDispatcher`？看启动日志。
- broker 是否长时间不可达？看 dispatcher 日志是否反复 `MarkFailed`。
- 死信表有累积？检查死信表是否有"`RetryCount > MaxRetries`"的行，人工介入。

### Q: Kafka 一个 handler 失败，相同 partition 的所有后续消息都卡住？

**会卡，但有上限**。`KafkaMessageSubscriber` 把失败聚合 `AggregateException` 上抛 → `DefaultKafkaMessageConsumer.PollLoop` 跳过 commit + `Seek(TopicPartitionOffset)` + 按 `FailureBackoff`（默认 5 秒）退避 → 下一轮 `Consume()` 重投同条消息。

**两层 poison message 自动截断**：

1. **反序列化失败**（`JsonException`：schema 不兼容、payload 损坏）：subscriber 立即 LogError + commit 跳过（消息字节固定，重试也是同样异常）。
2. **handler 持续失败**：同 offset 失败次数达 `MaxConsecutiveFailures`（默认 5）后，框架 LogWarning + commit 跳过。设 `0` 关闭层 2（无限重试）。

配合 inbox：**已成功处理的 handler 在 inbox 命中后会被跳过**，重投只重跑失败那个 handler。Inbox + `MaxConsecutiveFailures` 组合让"瞬时故障可恢复 + poison 不卡死 partition"两个目标都达成。

**再精细的"丢失可观测"**：靠日志监控两条 warning（`Kafka 消息连续失败 N 次,放弃重试 commit 跳过` 与 `Kafka payload 反序列化失败,跳过该条 offset`），或在 handler 内显式落 dead-letter topic 后吞掉异常让 PollLoop 视作成功。

### Q: RabbitMQ 一个 handler 失败，整条消息都重投？

会按 `FailureBehavior` 决定：
- `RequeueOnce`（默认）：首次失败时 broker 重投一次；二次失败 `nack(requeue=false)` 进 DLX 或丢弃。配合 inbox：**已成功的 handler 在 inbox 命中后跳过**，二次只跑失败那个 handler。
- `NackNoRequeue`：失败立即 `nack(requeue=false)`。
- `AlwaysAck`：完全依赖 inbox 兜底，不重投。

单条消息内多个 handler **不是"全失败才重投"**：只要任一 handler 抛异常，订阅器循环结束就聚合 `AggregateException` 上抛，触发 broker 层的 nack。这里依赖 inbox 的 `(messageId, handlerType)` 去重粒度，让重投只跑那个失败的 handler。

### Q: 想让本地事件和发布者同事务？

**可以，且这就是默认行为**。`LocalMessagePublisher` 注册为 Scoped，`LocalMessageHandlerInvoker` 用调用方 scope 的 SP 直接 resolve handler — handler 内拿到的 `DbContext` 会通过 `IDbContextProvider` 自动加入外层 UoW，`SaveChanges` 在 `UoW.CommitAsync` 时一并触发。
- handler 抛异常 → `LocalMessagePublisher` 聚合 `AggregateException` 上抛 → `UoW.CommitAsync` 失败 → 业务回滚（含 handler 写入）。
- 发布者后续业务失败回滚 → handler 已加入外层 UoW 的写入也跟着回滚。

唯一例外是 broker 路径（集成事件）：`InboxAwareMessageHandlerInvoker` 在 broker 消费侧会**开新 scope + 新 UoW**，因为消费端没有"发布者业务事务"可加入。

如果业务想要 best-effort 语义（handler 失败不影响发布者业务），请在 handler 内部 try-catch 吞掉异常。

### Q: 单元测试里如何 mock？

`ILocalPublisher` / `IIntegrationPublisher` 都是简单接口，直接 mock 即可。生产代码统一注入接口（不要注入具体实现类），是为了让这一步零摩擦。

---

## 架构内幕

### 三层架构

```
┌──────────────────────────────────────────────────────────────┐
│ 业务层                                                       │
│   IIntegrationPublisher / ILocalPublisher / IMessageHandler  │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│ 集成层 (Core.EventBus + Storage.EfCore)                      │
│   IntegrationMessagePublisherBase ─→ outbox 路由判定         │
│   InboxAwareMessageHandlerInvoker  ─→ broker 端幂等 + 新 UoW │
│   LocalMessageHandlerInvoker       ─→ 本地端复用调用方 scope │
│   EventBusBackgroundService        ─→ 启动期程序集扫描       │
│   OutboxDispatcher                 ─→ 后台投递循环           │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│ 基础设施层 (Local / Core.RabbitMQ / Core.Kafka)              │
│   LocalMessagePublisher / Subscriber                         │
│   RabbitMqPersistentConnection + ConsumerManager             │
│   KafkaPersistentProducer + ConsumerManager                  │
└──────────────────────────────────────────────────────────────┘
```

### 启动期事件流

1. `EventBusBackgroundService.StartAsync` → 扫 `MessageHandlerAssemblies`：
2. 反射出所有 `IMessageHandler<T>` 具体类型；
3. 把 `(MessageType, HandlerType)` 配对推给：
   - `ILocalSubscriber`（进程内派发表）
   - `IIntegrationSubscriber`（RabbitMQ bind / Kafka subscribe）
4. broker 准备好后开始接收消息。

### 运行期消息流（带 outbox + inbox）

**发布：**

```
业务调 PublishAsync
  ├─ 在 UoW 内？ → IOutboxStorage.StoreMessageAsync（业务行 + outbox 行同事务）
  │                ↓
  │              UoW.Commit
  │                ↓
  │              OutboxDispatcher 后台拉取 → IOutboxRawSender.SendRawAsync → broker
  │
  └─ 不在 UoW？ → 直发 broker（best-effort，无一致性保证）
```

**消费：**

```
broker 推消息
  ↓
RabbitMqMessageSubscriber / KafkaMessageSubscriber 反序列化
  ↓
InboxAwareMessageHandlerInvoker：
  ├─ inbox 命中 (messageId, handlerType)？ → 跳过（已处理）
  └─ 未命中 → 开 UoW
                ↓
              调 handler.HandleAsync
                ↓
              写 inbox 行
                ↓
              UoW.Commit（handler 写库 + inbox 行同事务）
                ↓
              broker ack（RabbitMQ）/ Commit offset（Kafka）
```

### publisher 注册为 Scoped

`IntegrationMessagePublisherBase` 及派生（`RabbitMqMessagePublisher` / `KafkaMessagePublisher`）注册为 **Scoped**：`PublishAsync` 在 outbox 路径要解析 `IOutboxStorage`（Scoped），storage 经 `IDbContextProvider` 拿到的 `DbContext` 必须由当前 UoW 所在 scope 持有，否则 publisher 返回后 scope 释放，外层 UoW 拿到 disposed `DbContext`，commit 时 SaveChanges 直接炸、outbox 行同步消失。

**对调用方的影响**：

| 调用上下文 | 怎么注入 |
|---|---|
| Controller / Application Service / Repository | 直接注入 `IIntegrationPublisher` |
| `BackgroundService` / `IHostedService` / 自定义 Singleton | **不能**直接注入；改注入 `IServiceScopeFactory`，运行时开 scope：<br/>`using var scope = scopeFactory.CreateScope();`<br/>`var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationPublisher>();`<br/>`await publisher.PublishAsync(evt);` |
| `OutboxDispatcher` / `InboxAwareMessageHandlerInvoker` | 框架已自建 scope，无需关心 |

### 不支持同时启用多个 integration broker

`AddRabbitMq()` 和 `AddKafka()` 互斥：同一进程同时调用，第二个会抛：

```
InvalidOperationException: 已注册 IIntegrationPublisher = RabbitMqMessagePublisher;
EventBus 同一时刻仅支持一个 integration broker,请只调用 AddRabbitMq / AddKafka 之一。
```

同 broker 重复调用幂等放行。要同时写两类 broker，请在业务侧自建独立 producer。

### outbox 行的两个 Id

`MessageEnvelope` 上有两个独立 Guid：

| 字段 | 来源 | 用途 |
|---|---|---|
| `Id` | 写入 outbox 时新生成 | outbox / 死信表行主键，dispatcher 内部寻址 |
| `MessageId` | 业务 `IMessage.Id` | broker header MessageId / Kafka partition key / inbox 去重键 |

直发路径同样用业务 `IMessage.Id` 作 broker MessageId，两条路径语义一致，运维按 broker MessageId 跨直发 / outbox 路径关联同一条事件。

### 故意没做的事

- **没有内置 saga / process manager。** 需要长事务 / 补偿的场景请用专门的 saga 框架。
- **没有内置事件 schema 版本管理。** payload 是 JSON，靠 `[MessageName]` 稳定路由 + 业务侧契约管理。
- **没有内置 dead letter 自动重投 UI。** 死信表暴露给运维，重投策略业务自决（写脚本 / 写后台任务）。
- **没有内置 RabbitMQ DLX → DLQ 绑定。** 框架只在 queue 声明里写 `x-dead-letter-exchange`；把死信路由到具体 DLQ（归档 / 人工介入 / 转其它系统）是运维侧的事，因为这部分语义跟业务强相关。
- **Kafka 没用 Schema Registry。** 默认 Newtonsoft.Json + UTF-8 字节流。需要 Avro / Protobuf 请在业务层自己包一层。
- **Kafka 没做 dead-letter topic。** Kafka 不支持单条 nack，业务上要丢可观测的失败消息请在 handler 内显式 `IKafkaPersistentProducer.ProduceAsync` 到 dead-letter topic。
