# Core.Kafka 使用手册

`Core.Kafka` 是 CoreFramework 中的 Kafka 基础设施类库，对应 `Core.RabbitMQ` 在 RabbitMQ 一侧的角色。封装：

- Kafka 持久生产者：`IKafkaPersistentProducer`（进程内长生命周期复用）
- Kafka 消费者及 manager：`IKafkaMessageConsumer` / `IKafkaMessageConsumerManager`（按 consumer group 复用）
- Topic 声明配置：`KafkaTopicDeclareConfigure`（可选显式建 topic）
- DI 注册扩展：`services.AddKafka(...)`
- Core.Modularity 模块：`CoreKafkaModule`

它是底层基础库，不负责消息对象序列化、发布者抽象、业务 handler 分发、outbox / inbox 等上层能力。这些能力由上层模块自行组合，例如 `Core.EventBus.Kafka` 会基于本库实现集成事件发布与订阅。

---

## 目录

- [适用场景](#适用场景)
- [依赖与目标框架](#依赖与目标框架)
- [核心类型总览](#核心类型总览)
- [配置说明](#配置说明)
- [注册方式](#注册方式)
- [快速开始：发布消息](#快速开始发布消息)
- [快速开始：消费消息](#快速开始消费消息)
- [Topic 声明](#topic-声明)
- [Producer 实现细节](#producer-实现细节)
- [Consumer 实现细节](#consumer-实现细节)
- [失败与 offset 语义](#失败与-offset-语义)
- [生命周期与释放](#生命周期与释放)
- [生产建议](#生产建议)
- [常见问题](#常见问题)
- [完整示例](#完整示例)
- [当前实现边界](#当前实现边界)

---

## 适用场景

适合使用本库的场景：

- 应用直接使用 Confluent.Kafka 客户端，但希望复用统一的长连接 producer、按 group 复用 consumer、DI 注册、AdminClient 建 topic。
- 你要自己控制消息协议、序列化格式、partition key 之外的业务语义。
- 上层模块需要一个基础 Kafka producer + consumer manager。

不适合只用本库解决的场景：

- 需要完整事件总线、自动 handler 分发、outbox / inbox。应使用上层 `Core.EventBus.Kafka`。
- 需要 schema registry（Avro / Protobuf）。本库默认 `byte[]` payload，请在业务层自己包一层序列化。
- 需要 dead-letter topic 自动落地。Kafka 不支持单条 nack；本库回调失败后会强制 commit offset，业务侧自己写 dead-letter 投递。
- Exactly-once（事务性 producer + read-process-write）。本库的 producer 开了 `EnableIdempotence` 但没启用事务，仍是 at-least-once。

---

## 依赖与目标框架

项目文件：`Core.Kafka.csproj`

主要依赖：

| 依赖 | 用途 |
|---|---|
| `Confluent.Kafka` | Kafka 原生客户端 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI 注册 |
| `Microsoft.Extensions.Options` | Options 配置 |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | 从 `IConfiguration` 绑定 options |
| `Microsoft.Extensions.Logging.Abstractions` | 日志抽象 |
| `Core.Modularity` | 模块化注册入口 |

版本由仓库根目录的 `Directory.Packages.props` 统一管理。

---

## 核心类型总览

| 类型 | 生命周期 | 职责 |
|---|---:|---|
| `KafkaOptions` | Options | 根配置对象。承载 `Connection`、`FailureBackoff`（失败 Seek 重投前的退避，默认 5s）、`MaxConsecutiveFailures`（同 offset 连续失败上限，默认 5；命中后 commit 跳过） |
| `KafkaConnectionConfigure` | Options 子对象 | bootstrap servers + SASL / TLS，`BuildClientConfig()` 产出基础 `ClientConfig` |
| `IKafkaPersistentProducer` | Singleton | 进程内长生命周期 producer，复用同一个 `IProducer<string, byte[]>` |
| `DefaultKafkaPersistentProducer` | Singleton | 默认 producer 实现：lazy build + `EnableIdempotence` + `Acks=All` + 内置 5 次重试 |
| `IKafkaMessageConsumerManager` | Singleton | 按 `groupId` 复用 consumer 实例 |
| `DefaultKafkaMessageConsumerManager` | Singleton | 默认 manager；同 group 只建一个 consumer，可选 `AdminClient` 显式建 topic |
| `IKafkaMessageConsumer` | 由 manager 创建 | 一个 consumer group 实例；订阅 topic、注册回调、poll 循环 |
| `DefaultKafkaMessageConsumer` | internal | 默认 consumer 实现，自带 `LongRunning` 任务跑 PollLoop |
| `KafkaTopicDeclareConfigure` | 普通对象 | 显式建 topic 用，承载 partition / replication factor / topic 级 broker 配置 |
| `KafkaServiceCollectionExtensions` | 静态扩展 | 提供 `AddKafka` 注册入口 |
| `CoreKafkaModule` | 模块 | 从配置节 `Kafka` 注册本库服务 |

---

## 配置说明

### appsettings.json

使用 `CoreKafkaModule` 时，默认读取 `Kafka` 节点：

```json
{
  "Kafka": {
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
```

直接调用 `services.AddKafka(Configuration.GetSection("Kafka"))`，配置结构相同。

### 字段说明

| 字段 | 必填 | 默认 | 说明 |
|---|---:|---|---|
| `FailureBackoff` | 否 | `00:00:05` | handler 抛异常时 PollLoop Seek 回 offset 重投前的退避，避免热循环 |
| `MaxConsecutiveFailures` | 否 | `5` | 同条 offset 连续失败上限。达到后 commit + LogWarning 跳过，避免 poison message 永久阻塞 partition。设 `0` 关闭（无限重试） |
| `Connection.BootstrapServers` | 是 | — | broker 列表，逗号分隔，例：`kafka-1:9092,kafka-2:9092`。等价于 RabbitMQ 那边 `HostName` 的 `;` 分隔形式 |
| `Connection.SecurityProtocol` | 否 | — | `Plaintext` / `Ssl` / `SaslPlaintext` / `SaslSsl` |
| `Connection.SaslMechanism` | 否 | — | `Plain` / `ScramSha256` / `ScramSha512` 等 |
| `Connection.SaslUsername` / `SaslPassword` | 否 | — | SASL 凭据 |

> `BootstrapServers` 留空时 `BuildClientConfig()` 会立即抛 `InvalidOperationException` 并给出可读提示（"请在 appsettings.json 的 Kafka:Connection:BootstrapServers 节点设置 broker 地址"），而不是把用户扔到 librdkafka 的 `ConfigException` 里猜配错了哪里。

### 共享 `ClientConfig`

`KafkaConnectionConfigure.BuildClientConfig()` 产出一个基础 `ClientConfig`，Producer 和 Consumer 各自在它之上叠加自己的专属配置（如 `Acks`、`EnableIdempotence`、`GroupId` 等）。需要更细的 librdkafka 调整（`linger.ms`、`batch.size`、`compression.type`、`socket.timeout.ms` 等）时，可派生 `KafkaConnectionConfigure` 重写 `BuildClientConfig()`。

---

## 注册方式

### 方式一：直接注册 IServiceCollection

```csharp
builder.Services.AddKafka(builder.Configuration.GetSection("Kafka"));
```

或代码配置：

```csharp
builder.Services.AddKafka(options =>
{
    options.Connection.BootstrapServers = "kafka:9092";
    options.Connection.SecurityProtocol = SecurityProtocol.SaslSsl;
    options.Connection.SaslMechanism = SaslMechanism.ScramSha512;
    options.Connection.SaslUsername = "app";
    options.Connection.SaslPassword = "***";
});
```

注册后容器中会有：

```csharp
IKafkaPersistentProducer
IKafkaMessageConsumerManager
IOptions<KafkaOptions>
```

使用 `TryAddSingleton`，如果你在调用前已经注册自定义实现，本库不会覆盖。

### 方式一·补：只挂 infrastructure，不绑配置

如果上层模块要自己接管 `KafkaOptions` 的来源（典型如 `Core.EventBus.Kafka` 用 `AddOptions<KafkaOptions>().Configure<IOptions<EventBusKafkaOptions>>(...)` 把 EventBus 自家 options 的 `Broker` 字段联动过来），可以调无参重载：

```csharp
services.AddKafka();   // 只 TryAddSingleton 两个 infrastructure 服务
```

此时本库不向容器注入任何 `Configure<KafkaOptions>(...)`；调用方需要自己提供至少一个 Options 配置源（`Configure` / `AddOptions...Configure<IOptions<...>>` / `PostConfigure` 等），否则首次解析 `IOptions<KafkaOptions>.Value` 时 `BuildClientConfig()` 会因 `BootstrapServers` 为空抛 `InvalidOperationException`。

### 方式二：通过 Core.Modularity 模块注册

`CoreKafkaModule` 会在 `ConfigureServices` 中执行 `services.AddKafka(Configuration.GetSection("Kafka"))`。

```csharp
using Core.Kafka;
using Core.Modularity;
using Core.Modularity.Attribute;

[DependsOn(typeof(CoreKafkaModule))]
public class MyAppModule : CoreModuleBase
{
}
```

---

## 快速开始：发布消息

本库不提供 publisher 抽象，发布时直接注入 `IKafkaPersistentProducer`。

```csharp
using System.Text;
using Confluent.Kafka;
using Core.Kafka;

public sealed class OrderMessagePublisher
{
    private readonly IKafkaPersistentProducer _producer;

    public OrderMessagePublisher(IKafkaPersistentProducer producer) => _producer = producer;

    public async Task PublishAsync(string orderJson, Guid orderId, CancellationToken ct)
    {
        var msg = new Message<string, byte[]>
        {
            // partition key:同一 key 落同一 partition,保证序
            Key = orderId.ToString(),
            Value = Encoding.UTF8.GetBytes(orderJson),
            Headers = new Headers
            {
                { "messageId", Encoding.UTF8.GetBytes(orderId.ToString()) },
            },
        };

        var report = await _producer.ProduceAsync("orders.created", msg, ct);
        if (report.Status == PersistenceStatus.NotPersisted)
        {
            throw new InvalidOperationException(
                $"Kafka message not persisted: messageId={orderId} topic={report.Topic}");
        }
    }
}
```

要点：

- `ProduceAsync` 在 `Acks=All` 下会等所有 ISR 副本确认才返回。`PersistenceStatus.NotPersisted` 表示未落任何 broker，需要业务上抛出。
- `Produce`（非 async）是 fire-and-forget：回调里只 log，业务路径用 `ProduceAsync`。
- 进程退出前调 `_producer.Flush(TimeSpan.FromSeconds(5))` 让 in-flight 消息送达（`Dispose` 内部也做了一次，保护 worker 优雅退出场景）。

---

## 快速开始：消费消息

消费入口是 `IKafkaMessageConsumerManager`，一个 `groupId` 对应一个 consumer 实例。

```csharp
using System.Text;
using Confluent.Kafka;
using Core.Kafka;

public sealed class OrderMessageConsumerHostedService : IHostedService
{
    private readonly IKafkaMessageConsumerManager _manager;
    private readonly ILogger<OrderMessageConsumerHostedService> _logger;
    private IKafkaMessageConsumer _consumer;

    public OrderMessageConsumerHostedService(
        IKafkaMessageConsumerManager manager,
        ILogger<OrderMessageConsumerHostedService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 不传 topicDeclare 时信任 broker 的 auto.create.topics.enable
        _consumer = _manager.TryCreate("order-service");

        _consumer.OnMessageReceived(async (consumer, result) =>
        {
            var json = Encoding.UTF8.GetString(result.Message.Value);
            _logger.LogInformation("Received order topic={Topic} key={Key} body={Body}",
                result.Topic, result.Message.Key, json);

            await HandleAsync(json);
            // 不要手动 Commit / StoreOffset;PollLoop 处理完所有回调后会自动 commit
        });

        await _consumer.SubscribeTopicAsync("orders.created");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // manager.TryRemove 后 consumer 被 Dispose;Dispose 内部会 Cancel poll、等 5s、Close、Dispose
        _manager.TryRemove("order-service");
        return Task.CompletedTask;
    }

    private static Task HandleAsync(string json) => Task.CompletedTask;
}
```

注册：

```csharp
builder.Services.AddHostedService<OrderMessageConsumerHostedService>();
```

要点：

- `TryCreate(groupId)` 同 group 多次调用复用同一个 consumer 实例。
- `SubscribeTopicAsync(topic)` 把 topic 加入订阅集合并触发 group 重平衡。**真正调 `_consumer.Subscribe()` 由 PollLoop 在自己的线程上同步执行**（Confluent.Kafka 文档明确禁止 Subscribe 与 Consume 跨线程并发），订阅变更通过 `_subscriptionDirty` 位挂账，下一轮 poll 时一次性应用。
- `OnMessageReceived` 注册回调；同一方法不会重复注册（用 `Delegate.Equals` 比对 `Target + Method`）。
- **不要在回调内手动 Commit/StoreOffset**，PollLoop 会在所有回调跑完后强制 commit（见[失败与 offset 语义](#失败与-offset-语义)）。

---

## Topic 声明

Confluent.Kafka 的 broker 默认 `auto.create.topics.enable=true`，可不显式建 topic。但生产环境通常关掉自动建 topic，需要业务层显式声明。

### KafkaTopicDeclareConfigure

```csharp
public KafkaTopicDeclareConfigure(
    string topicName,
    int numPartitions = 3,
    short replicationFactor = 1,
    Dictionary<string, string> configs = null)
```

字段：

| 参数 | 默认 | 说明 |
|---|---|---|
| `NumPartitions` | 3 | partition 数决定并发上限：一个 partition 最多被一个 consumer 实例消费 |
| `ReplicationFactor` | 1 | 副本因子。生产至少 2，配合 `Acks=All` 才能保证落盘安全 |
| `Configs` | 空 | topic 级 broker 配置，如 `retention.ms`、`cleanup.policy` |

### 触发显式建 topic

把 `KafkaTopicDeclareConfigure` 传给 `TryCreate`：

```csharp
var topicDeclare = new KafkaTopicDeclareConfigure(
    topicName: "orders.created",
    numPartitions: 6,
    replicationFactor: 3,
    configs: new Dictionary<string, string>
    {
        ["retention.ms"] = "604800000",   // 7 天
        ["cleanup.policy"] = "delete",
    });

var consumer = _manager.TryCreate("order-service", topicDeclare);
```

实现细节：

- manager 内部用 `AdminClient.CreateTopicsAsync` 提交，`OperationTimeout` / `RequestTimeout` 都设了 **5 秒上限**，避免 broker 不可达时启动期被卡到默认 60 秒。
- 已存在的 topic：捕获 `CreateTopicsException` 中 `TopicAlreadyExists` → 仅 LogTrace。
- broker 不可达 / 权限不足 → LogWarning，**降级为信任 broker 的 `auto.create.topics.enable`**。

> Topic 声明对 consumer 实例的 key 没有影响：同 `groupId` 只建一次 consumer，topicDeclare 只是触发一次 AdminClient 调用。

---

## Producer 实现细节

默认实现：`DefaultKafkaPersistentProducer`。

### 关键 producer 配置

| 配置 | 值 | 原因 |
|---|---|---|
| `EnableIdempotence` | `true` | broker 侧基于 producer id + sequence 去重，避免网络重试导致同一条消息落 broker 两次。这是 outbox 至少一次语义的天然补充 |
| `Acks` | `All` | 等所有 ISR 副本确认才返回。前提是 topic 复制因子 ≥ 2 |
| `MessageSendMaxRetries` | `5` | 内置重试 + idempotence 不会重复 |

其他 librdkafka 默认值（`linger`、`batch.size`、`compression.type`）保留为默认，调优让给用户在 broker 侧或自定义 `KafkaConnectionConfigure` 时调整。

### 懒构造

`Producer` 属性用 double-checked-locking 模式懒构造。原因：`KafkaOptions` 在 DI 注册时还没填，Singleton 实例化时直接构造 producer 会拿到空配置抛 librdkafka 异常。第一次 `Produce`/`ProduceAsync` 调用时才真正建。

### Dispose

退出前调 `Flush(TimeSpan.FromSeconds(5))` 给 in-flight 消息一个有限时间送达，避免进程退出时丢消息。

---

## Consumer 实现细节

默认实现：`DefaultKafkaMessageConsumer`，一个实例对应一个 consumer group。

### 关键 consumer 配置

| 配置 | 值 | 原因 |
|---|---|---|
| `EnableAutoCommit` | `false` | 由 `PollLoop` 手动控制 commit 时机 |
| `EnableAutoOffsetStore` | `false` | 同上 |
| `AutoOffsetReset` | `Earliest` | 首次 join group 时从最早 offset 读，避免"上线前事件被静默吞掉" |
| `PartitionAssignmentStrategy` | `CooperativeSticky` | rebalance 时尽量保留之前的 partition 分配，缩短停顿 |
| `EnablePartitionEof` | `false` | 不向上抛 EOF 事件 |

### Subscribe / Consume 跨线程的约束

Confluent.Kafka 的 consumer **必须由同一个线程调用 `Subscribe()` / `Consume()`**，跨线程操作 librdkafka consumer 是未定义行为。本库的方案：

- `SubscribeTopicAsync(topic)` / `UnsubscribeTopicAsync(topic)` **只把 topic 加进/移出 `_subscribedTopics`，并翻一下 `_subscriptionDirty` 位**。
- `PollLoop` 每轮顶部检查 `_subscriptionDirty`，如果有变更就在 poll 线程上调 `_consumer.Subscribe()` 把订阅集合刷上去。

### PollLoop

```text
while (!cancelled) {
    if (订阅有变更) ApplySubscriptionOnPollThread();
    if (订阅空)     idle sleep 5s; continue;

    var result = _consumer.Consume(ActivePollInterval);   // 1s
    if (result == null) continue;

    foreach (var handler in _processEvents)
        try { await handler(_consumer, result); }
        catch (Exception ex) { log; 继续下一个 handler }

    // 无论 handler 成败 → commit offset
    _consumer.StoreOffset(result);
    _consumer.Commit(result);
}
```

- 空订阅时 sleep 5 秒，避免对空 consumer 反复 syscall。
- 有订阅时 `Consume(1s)`，broker 推消息时几乎不会真正等满。
- 非预期异常（如 `InvalidOperationException`）：log + 退避 1 秒后**继续循环**（旧版本会让 PollLoop 退出，consumer 永久停摆）。
- PollLoop 用 `Task.Factory.StartNew(..., TaskCreationOptions.LongRunning).Unwrap()`：占线程时长不可控的阻塞 `Consume()` 不应抢线程池；`Unwrap` 后 `Dispose` 的 `Wait` 能等到 PollLoop 真正 finally 退出。

---

## 失败与 offset 语义

### 一句话

**任一 processEvent 抛异常** → PollLoop 跳过 commit + `Seek(TopicPartitionOffset)` 回失败 offset + 按 `FailureBackoff` 退避（默认 5 秒）→ 下一轮 `Consume()` 重投同条消息。配合上层 inbox 去重达成"业务最终一致"。

### 为什么 Seek 不是只"跳过 commit"

仅"不 commit" 在单进程里没用 —— librdkafka 内部 cursor 已经因为本次 `Consume` 前进，broker 端 offset 不动也不会让 consumer 自己倒回。下一次 `Consume()` 仍会拉下一条。

必须 `Seek(result.TopicPartitionOffset)` 强制把内部 cursor 拨回，才能在同一进程里让 broker 重投同条消息。

```text
offset 5  失败  → Seek(5) + Delay(FailureBackoff) → continue
offset 5  重投  → 处理成功 → Commit(5)
offset 6  ...
```

### Poison message：两层自动截断

框架对 poison message 做两层自动处理：

**层 1 — 反序列化失败（`JsonException`）**：消息字节已固定，重试永远是同样异常。`KafkaMessageSubscriber.ProcessEvent` 立即 LogError + return（不抛）→ PollLoop 视作成功 commit + 推进 offset。`payload` 字段日志截断到 512 字符避免被异常大消息撑爆。

**层 2 — handler 异常（业务、依赖、网络等）**：按 `MaxConsecutiveFailures` 计数。

```text
offset 5  失败 1 次 → Seek(5) + Delay
offset 5  失败 2 次 → Seek(5) + Delay
offset 5  失败 3 次 → Seek(5) + Delay
offset 5  失败 4 次 → Seek(5) + Delay
offset 5  失败 5 次 → LogWarning + Commit(5) + 推进     (默认 MaxConsecutiveFailures=5)
offset 6  ...
```

上限计数器仅在 PollLoop 线程访问、按 `(topic, partition, offset)` 比对，offset 变化（成功推进或重平衡）时归零。`MaxConsecutiveFailures = 0` 关闭层 2（无限重试，等价旧语义）。

### 再精细的"丢消息可观测"

| 业务诉求 | 实现方式 |
|---|---|
| 至少一次（不丢）+ 幂等 | 上层 inbox：用 `(messageId, handlerType)` 表去重；成功 handler 在 inbox 命中后跳过，只重跑失败那个 |
| 失败消息可观测 | 在 handler 内显式 `_producer.ProduceAsync` 到 dead-letter topic + **吞掉异常** → PollLoop 视作成功 commit，不再卡 partition |
| 长期失败截断 + 告警 | 监控订阅 `Kafka 消息连续失败 {N} 次,放弃重试 commit 跳过` 与 `Kafka payload 反序列化失败,跳过该条 offset` 两条日志 |

### 与 RabbitMQ 实现对偶

- RabbitMQ：失败 → `nack(requeue=true)` → broker 端推进重投
- Kafka：失败 → consumer 端 Seek 回 offset → broker 端不变，消费者自拨

两者在业务层都是"失败 → 让 broker 在下一轮重投同条消息"。

---

## 生命周期与释放

### IKafkaPersistentProducer

DI 注册为 Singleton，由容器释放。`Dispose` 内部先 `Flush(5s)` 再释放底层 `IProducer`。

### IKafkaMessageConsumer

由 `IKafkaMessageConsumerManager` 创建。释放方式：

```csharp
manager.TryRemove(groupId);   // 内部会 Dispose
```

`Dispose` 流程：

1. `_cts.Cancel()` 让 PollLoop 退出。
2. `_pollLoop?.Wait(5s)` 等 PollLoop 真正 finally 出。
3. `_consumer.Close()` 提交未提交的 offset / leave group。
4. `_consumer.Dispose()`、`_cts.Dispose()`。

每一步都独立 try-catch，前一步抛异常不阻碍后续清理。

### Scoped 服务使用

回调签名是 `Func<IConsumer<string, byte[]>, ConsumeResult<string, byte[]>, Task>`，不会自动开 DI scope。需要 `DbContext` 等 scoped 服务时注入 `IServiceScopeFactory`，在回调内创建：

```csharp
_consumer.OnMessageReceived(async (consumer, result) =>
{
    using var scope = scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    // ...
    await db.SaveChangesAsync();
});
```

---

## 生产建议

### 发布端

- 业务路径用 `ProduceAsync`（拿到 `DeliveryResult` 决定后续动作）；fire-and-forget 才用 `Produce`。
- 用 message Id 作为 `Key`（partition key）：同 Id 消息有序、不同 Id 分散负载。
- topic 级别开启 `compression.type=zstd` / `lz4` 在带宽紧张时显著省流。
- 进程退出前主动 `Flush(5s)`，避免 in-flight 消息丢失。

### 消费端

- 业务处理**必须幂等**。重平衡、rebalance、handler 抛异常都可能让同条消息再处理。
- partition 数决定单 group 内并发上限。计算压力大的 topic 把 partition 数提前规划好（partition 数后续可加但不能减）。
- 不要在回调内手动 commit / store offset。
- 长时间运行的 handler 注意 `max.poll.interval.ms`（librdkafka 默认 5 分钟），超过会被踢出 group。

### 连接配置

- 生产环境一律 `SaslSsl` + SCRAM-SHA-512；明文只在本地开发用。
- `BootstrapServers` 多个 broker 都写进去，客户端自己做 metadata 发现。
- topic 复制因子 ≥ 2，否则 `Acks=All` 等于 `Acks=1`。

### 日志

默认实现挂了：

- producer / consumer 的 `ErrorHandler` → LogWarning（含 `IsFatal` 字段）
- producer / consumer 的 `LogHandler` → LogTrace
- PollLoop 启停 / 异常重试 / commit 失败 → LogInformation/Warning/Error

排查时打开 trace 级别能看到 librdkafka 内部日志。

---

## 常见问题

### 1. `BootstrapServers 未配置` 异常

`KafkaConnectionConfigure.BuildClientConfig()` 在 `BootstrapServers` 空时直接抛 `InvalidOperationException`，提示去 `Kafka:Connection:BootstrapServers` 设置。

### 2. consumer 一直没消息

按这个顺序排查：

1. broker 上对应 topic 真的有消息？
2. consumer group 的 offset 已经追上头？`AutoOffsetReset=Earliest` 只对**首次加入 group** 生效，之前已经 join 过的 group 会从已存 offset 继续。
3. 订阅是否成功？看日志里有没有 `Kafka consumer poll loop started`。`SubscribeTopicAsync` 后**下一轮 poll** 才会真正 Subscribe，启动后等 1~2 秒。
4. 是否有 partition 分配？SaslSsl 配置错时 broker 会拒绝 join group，看 `Kafka consumer error` 日志的 `IsFatal=true`。

### 3. handler 异常后消息会被重投吗？卡多久？

**会重投，但有上限**。PollLoop 跳过 commit + `Seek(TopicPartitionOffset)` + 退避（`FailureBackoff`，默认 5 秒）→ 下一轮 `Consume()` 重投同条消息。同条 offset 连续失败达 `MaxConsecutiveFailures`（默认 5）后，框架 LogWarning + commit 跳过，避免 partition 永久阻塞。详见[失败与 offset 语义](#失败与-offset-语义)。

旧版"失败也 commit、消息从 broker 视角消失"等价于把 `MaxConsecutiveFailures = 1`（首次失败就跳过）。但生产环境**不建议** —— inbox 在事务里回滚后没机会重试，业务等于丢消息。让 `MaxConsecutiveFailures` 给瞬时故障留几次重试空间。

### 4. PollLoop 异常退出导致 consumer 停摆

旧版本在 PollLoop 内的非预期异常会直接退出整个循环。**已修复**：非预期异常 → log + 退避 1 秒 → 继续循环。

### 5. Dispose 卡 5 秒以上

`Dispose` 给 PollLoop 留 5 秒等它真正 finally 退出，加上 `_consumer.Close()` 可能再花几秒提交 offset。如果发现卡更久：

- 看是否有 handler 在跑死循环导致 PollLoop 等不到结束；
- 看 broker 是否已经不可达（`Close` 内部要走 leave-group RPC）。

### 6. 多个回调注册了但只有一个执行

`OnMessageReceived` 用 `Delegate.Equals` 比对 `Target + Method`，**同方法同实例**会被去重。如果要多个不同 handler，用不同实例或不同方法：

```csharp
_consumer.OnMessageReceived(handler1.Process);
_consumer.OnMessageReceived(handler2.Process);   // 不同 Target → 都会注册
```

### 7. 显式建 topic 报权限不足

`TryDeclareTopic` 内部把所有 AdminClient 异常降级为 LogWarning，不阻断启动 —— 假设 broker 的 `auto.create.topics.enable` 兜底。

如果不希望降级，自己用 `AdminClient` 显式建：

```csharp
using var admin = new AdminClientBuilder(connection.BuildClientConfig()).Build();
await admin.CreateTopicsAsync(new[] { new TopicSpecification { ... } });
```

---

## 完整示例

### appsettings.json

```json
{
  "Kafka": {
    "Connection": {
      "BootstrapServers": "kafka-1:9092,kafka-2:9092,kafka-3:9092",
      "SecurityProtocol": "SaslSsl",
      "SaslMechanism": "ScramSha512",
      "SaslUsername": "app",
      "SaslPassword": "***"
    }
  }
}
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKafka(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<OrderMessagePublisher>();
builder.Services.AddHostedService<OrderMessageConsumerHostedService>();

var app = builder.Build();

app.MapPost("/orders", async (
    OrderMessagePublisher publisher,
    CreateOrderRequest request,
    CancellationToken ct) =>
{
    var json = System.Text.Json.JsonSerializer.Serialize(request);
    await publisher.PublishAsync(json, request.OrderId, ct);
    return Results.Accepted();
});

app.Run();

public sealed record CreateOrderRequest(Guid OrderId, decimal Amount);
```

### Publisher

```csharp
using System.Text;
using Confluent.Kafka;
using Core.Kafka;

public sealed class OrderMessagePublisher
{
    private readonly IKafkaPersistentProducer _producer;

    public OrderMessagePublisher(IKafkaPersistentProducer producer) => _producer = producer;

    public async Task PublishAsync(string json, Guid orderId, CancellationToken ct)
    {
        var msg = new Message<string, byte[]>
        {
            Key = orderId.ToString(),
            Value = Encoding.UTF8.GetBytes(json),
            Headers = new Headers
            {
                { "messageId", Encoding.UTF8.GetBytes(orderId.ToString()) },
            },
        };

        var report = await _producer.ProduceAsync("orders.created", msg, ct);
        if (report.Status == PersistenceStatus.NotPersisted)
        {
            throw new InvalidOperationException(
                $"Kafka message not persisted: orderId={orderId} topic={report.Topic}");
        }
    }
}
```

### Consumer

```csharp
using System.Text;
using Confluent.Kafka;
using Core.Kafka;

public sealed class OrderMessageConsumerHostedService : IHostedService
{
    private readonly IKafkaMessageConsumerManager _manager;
    private readonly ILogger<OrderMessageConsumerHostedService> _logger;
    private IKafkaMessageConsumer _consumer;

    public OrderMessageConsumerHostedService(
        IKafkaMessageConsumerManager manager,
        ILogger<OrderMessageConsumerHostedService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 生产环境通常显式声明 topic 让 partition / 副本数受控
        var topicDeclare = new KafkaTopicDeclareConfigure(
            topicName: "orders.created",
            numPartitions: 6,
            replicationFactor: 3);

        _consumer = _manager.TryCreate("order-service", topicDeclare);

        _consumer.OnMessageReceived(async (consumer, result) =>
        {
            var json = Encoding.UTF8.GetString(result.Message.Value);
            _logger.LogInformation(
                "Received order topic={Topic} key={Key} partition={Partition} offset={Offset} body={Body}",
                result.Topic, result.Message.Key, result.Partition, result.Offset, json);

            await Task.CompletedTask;
        });

        await _consumer.SubscribeTopicAsync("orders.created");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _manager.TryRemove("order-service");
        return Task.CompletedTask;
    }
}
```

---

## 当前实现边界

本库已经内置：

- ✅ 长生命周期 producer + idempotence + `Acks=All`
- ✅ 按 group 复用 consumer
- ✅ 可选显式建 topic（5 秒超时 + 降级到 broker auto-create）
- ✅ PollLoop 异常自愈、Dispose 5s 截断
- ✅ Subscribe / Consume 同线程约束（`_subscriptionDirty` 位）
- ✅ CooperativeSticky 重平衡策略

以下能力仍未内置：

- 消息序列化协议（自带 `byte[]` payload）
- publisher / subscriber 高层抽象
- handler 自动发现 + 分发
- outbox / inbox
- dead-letter topic 自动落地
- Exactly-once（事务 producer + read-process-write）
- Schema Registry（Avro / Protobuf）
- 每条消息自动创建 DI scope
- 自定义 Polly 重试策略（producer 已用 librdkafka 内置 retry，不需要 Polly）

如果项目需要这些能力，优先考虑使用或扩展上层 `Core.EventBus.Kafka`，或者在本库之上实现自己的业务封装。
