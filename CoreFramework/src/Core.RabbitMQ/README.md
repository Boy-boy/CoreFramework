# Core.RabbitMQ 使用手册

`Core.RabbitMQ` 是 CoreFramework 中的 RabbitMQ 基础设施类库，主要封装：

- RabbitMQ 持久连接管理：`IRabbitMqPersistentConnection`
- RabbitMQ 消费者创建与缓存：`IRabbitMqMessageConsumerManager` / `IRabbitMqMessageConsumer`
- Exchange / Queue 声明配置：`RabbitMqExchangeDeclareConfigure`、`RabbitMqQueueDeclareConfigure`
- 发布端 channel 池 + publisher confirms：`IRabbitMqPublishChannelPool` / `RabbitMqPublishChannelPool` / `PooledChannel`
- 失败信号统一异常：`RabbitMqPublishFailedException`（派生 `RabbitMqPublishReturnedException` / `RabbitMqPublishUnconfirmedException`）
- 消费端失败策略枚举：`RabbitMqFailureBehavior`（`RequeueOnce` / `NackNoRequeue` / `AlwaysAck`）
- DI 注册扩展：`services.AddRabbitMq(...)`
- Core.Modularity 模块：`CoreRabbitMqModule`

它是底层基础库，不负责消息对象序列化、发布者抽象、业务 handler 分发、outbox / inbox、死信队列绑定等上层能力。这些能力由上层模块自行组合，例如 `Core.EventBus.RabbitMQ` 会基于本库实现集成事件发布与订阅。

---

## 目录

- [适用场景](#适用场景)
- [依赖与目标框架](#依赖与目标框架)
- [核心类型总览](#核心类型总览)
- [配置说明](#配置说明)
- [注册方式](#注册方式)
- [快速开始：发布消息](#快速开始发布消息)
- [发布端 channel 池 + publisher confirms](#发布端-channel-池--publisher-confirms)
- [快速开始：消费消息](#快速开始消费消息)
- [消费失败策略 FailureBehavior 与 DLX](#消费失败策略-failurebehavior-与-dlx)
- [Exchange 与 Queue 声明](#exchange-与-queue-声明)
- [连接管理实现细节](#连接管理实现细节)
- [消费者实现细节](#消费者实现细节)
- [生命周期与释放](#生命周期与释放)
- [生产建议](#生产建议)
- [常见问题](#常见问题)
- [完整示例](#完整示例)

---

## 适用场景

适合使用本库的场景：

- 应用需要直接使用 RabbitMQ.Client，但希望复用统一的连接管理、重试、DI 注册和消费通道维护。
- 你要自己控制消息协议、序列化格式、routing key、ack 语义之外的业务处理。
- 上层模块需要一个基础 RabbitMQ 连接与消费者管理组件。

不适合只用本库解决的场景：

- 需要完整事件总线、自动消息分发、handler 注册、outbox / inbox。应使用上层 `Core.EventBus.RabbitMQ`。
- 需要内置重试队列、死信队列、延迟队列、失败消息持久化。本库只提供底层接入能力。
- 需要复杂消费失败策略。本库当前只做自动 ack 的基本消费流，失败策略需要调用方或上层模块设计。

---

## 依赖与目标框架

项目文件：`Core.RabbitMQ.csproj`

主要依赖：

| 依赖 | 用途 |
|---|---|
| `RabbitMQ.Client` | RabbitMQ 原生客户端 |
| `Polly` | 连接重试策略 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI 注册 |
| `Microsoft.Extensions.Options` | Options 配置 |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | 从 `IConfiguration` 绑定 options |
| `Microsoft.Extensions.Logging.Abstractions` | 日志抽象 |
| `Core.Modularity` | 模块化注册入口 |

版本由仓库根目录的 `Directory.Packages.props` 统一管理。当前仓库中 `RabbitMQ.Client` 使用 `6.8.1`，`Polly` 使用 `8.6.5`。

---

## 核心类型总览

| 类型 | 生命周期 | 职责 |
|---|---:|---|
| `RabbitMqOptions` | Options | 根配置对象。承载 `Connection`、`FailureBehavior`、`DeadLetterExchange`、`DeadLetterRoutingKey` |
| `RabbitMqFailureBehavior` | enum | 消费失败策略：`AlwaysAck` / `NackNoRequeue` / `RequeueOnce`（默认） |
| `RabbitMqConnectionConfigure` | Options 子对象 | Host、Port、UserName、Password、VirtualHost，并生成 `ConnectionFactory` |
| `IRabbitMqPersistentConnection` | Singleton | 持久连接抽象，负责连接、重连、创建 channel |
| `DefaultRabbitMqPersistentConnection` | Singleton | 默认持久连接实现，含 Polly 6 次指数退避 + dispose-safe |
| `IRabbitMqPublishChannelPool` | Singleton（由上层注册） | publisher 复用 channel 池契约 |
| `RabbitMqPublishChannelPool` | sealed | 默认池实现：`SemaphoreSlim` 限并发 + `ConcurrentQueue` 维护空闲 channel |
| `PooledChannel` | readonly struct | 租赁句柄；`Dispose()` 归还，`WaitForConfirmsOrThrow(timeout)` 把退回/nack/超时转 `RabbitMqPublishFailedException` |
| `RabbitMqPublishFailedException` | exception | 发布失败基类 |
| `RabbitMqPublishReturnedException` | exception | mandatory:true 时 broker 无路由退回 |
| `RabbitMqPublishUnconfirmedException` | exception | confirm 超时或 broker nack |
| `RabbitMqExchangeDeclareConfigure` | 普通对象 | Exchange 声明参数与 `Declare(IModel)` |
| `RabbitMqQueueDeclareConfigure` | 普通对象 | Queue 声明参数与 `Declare(IModel)` |
| `IRabbitMqMessageConsumerManager` | Singleton | 按 (exchange, queue) 维护 consumer 实例；`TryRemove` 会自动 `Dispose` |
| `DefaultRabbitMqMessageConsumerManager` | Singleton | 默认 consumer 管理器 |
| `IRabbitMqMessageConsumer` | 由 manager 创建 | 绑定 / 解绑 routing key，注册消息回调，释放消费通道 |
| `DefaultRabbitMqMessageConsumer` | internal | 默认 consumer 实现，按 `FailureBehavior` 决定 ack/nack |
| `RabbitMqServiceCollectionExtensions` | 静态扩展 | 提供 `AddRabbitMq` 注册入口 |
| `CoreRabbitMqModule` | 模块 | 从配置节 `RabbitMq` 注册本库服务 |

> `IRabbitMqPublishChannelPool` 的实现是通用基础设施，但**默认 DI 注册由上层模块完成**（参考 `Core.EventBus.RabbitMQ.CoreEventBusRabbitMqModule`），因为池需要 exchange 名 + pool 大小这两个业务参数。直接使用 `Core.RabbitMQ` 的调用方可自行注册（见 [发布端 channel 池](#发布端-channel-池--publisher-confirms)）。

---

## 配置说明

### appsettings.json

使用 `CoreRabbitMqModule` 时，默认读取 `RabbitMq` 节点：

```json
{
  "RabbitMq": {
    "FailureBehavior": "RequeueOnce",
    "DeadLetterExchange": "myapp.dlx",
    "DeadLetterRoutingKey": null,
    "Connection": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/"
    }
  }
}
```

如果直接调用 `services.AddRabbitMq(Configuration.GetSection("RabbitMq"))`，配置结构相同。

### 字段说明

| 字段 | 必填 | 默认 | 说明 |
|---|---:|---|---|
| `FailureBehavior` | 否 | `RequeueOnce` | 消费 handler 抛异常时的 ack/nack 决策（见[失败策略](#消费失败策略-failurebehavior-与-dlx)） |
| `DeadLetterExchange` | 否 | 空 | 设置后由调用方在 queue 声明里挂 `x-dead-letter-exchange`；本库消费器自身**不会**自动写入 queue 参数，写入由上层（如 `Core.EventBus.RabbitMQ`）完成 |
| `DeadLetterRoutingKey` | 否 | 空 | 仅 `DeadLetterExchange` 设置时生效；留空时复用原 routing key（适用 direct DLX） |
| `Connection.HostName` | 是 | — | RabbitMQ 主机名。支持用分号分隔多个节点，例如 `mq-1;mq-2;mq-3` |
| `Connection.Port` | 是 | — | RabbitMQ 端口，常用 `5672` |
| `Connection.UserName` | 是 | — | 用户名 |
| `Connection.Password` | 是 | — | 密码 |
| `Connection.VirtualHost` | 否 | — | 虚拟主机。为空时使用 RabbitMQ.Client 默认值 |

> `HostName` 留空时，`DefaultRabbitMqPersistentConnection.TryConnect()` 会立即抛 `InvalidOperationException` 并给出可读提示，而不是在 Polly 退避里反复抛 NRE。

### 集群 HostName 写法

`DefaultRabbitMqPersistentConnection` 会执行：

```csharp
var hostnames = options.Connection.HostName.TrimEnd(';').Split(';');
```

当只有一个 host 时调用 `CreateConnection()`；当有多个 host 时调用 `CreateConnection(hostnames)`，交给 RabbitMQ.Client 处理连接选择。

推荐写法：

```json
{
  "RabbitMq": {
    "Connection": {
      "HostName": "mq-1;mq-2;mq-3",
      "Port": 5672,
      "UserName": "app",
      "Password": "secret",
      "VirtualHost": "my-vhost"
    }
  }
}
```

---

## 注册方式

### 方式一：直接注册 IServiceCollection

适合普通 ASP.NET Core / Worker Service。

```csharp
builder.Services.AddRabbitMq(
    builder.Configuration.GetSection("RabbitMq"));
```

或者用代码配置：

```csharp
builder.Services.AddRabbitMq(options =>
{
    options.Connection.HostName = "localhost";
    options.Connection.Port = 5672;
    options.Connection.UserName = "guest";
    options.Connection.Password = "guest";
    options.Connection.VirtualHost = "/";
});
```

注册后容器中会有：

```csharp
IRabbitMqPersistentConnection
IRabbitMqMessageConsumerManager
IOptions<RabbitMqOptions>
```

注册使用 `TryAddSingleton`，如果你在调用前已经注册自定义实现，本库不会覆盖已有实现。

### 方式一·补：只挂 infrastructure，不绑配置

如果上层模块要自己接管 `RabbitMqOptions` 的来源（典型如 `Core.EventBus.RabbitMQ` 用 `AddOptions<RabbitMqOptions>().Configure<IOptions<EventBusRabbitMqOptions>>(...)` 把 EventBus 自家 options 的 `Broker` 字段联动过来），可以调无参重载：

```csharp
services.AddRabbitMq();   // 只 TryAddSingleton 两个 infrastructure 服务
```

此时本库不向容器注入任何 `Configure<RabbitMqOptions>(...)`；调用方需要自己提供至少一个 Options 配置源（`Configure` / `AddOptions...Configure<IOptions<...>>` / `PostConfigure` 等），否则 `IOptions<RabbitMqOptions>.Value` 解析出的实例 `Connection.HostName` 为空，首次 `TryConnect()` 会抛 `InvalidOperationException`。

### 方式二：通过 Core.Modularity 模块注册

`CoreRabbitMqModule` 会在 `ConfigureServices` 中执行：

```csharp
context.Services.AddRabbitMq(Configuration.GetSection("RabbitMq"));
```

如果你的启动模块使用 Core.Modularity，可以声明依赖：

```csharp
using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;

[DependsOn(typeof(CoreRabbitMqModule))]
public class MyAppModule : CoreModuleBase
{
}
```

启动时通过框架已有入口加载模块即可：

```csharp
services.ConfigureServiceCollection<MyAppModule>();
```

---

## 快速开始：发布消息

本库不提供 publisher 抽象。发布消息时直接注入 `IRabbitMqPersistentConnection`，创建 channel 后使用 RabbitMQ.Client API。

```csharp
using System.Text;
using Core.RabbitMQ;
using RabbitMQ.Client;

public sealed class OrderMessagePublisher
{
    private readonly IRabbitMqPersistentConnection _connection;

    public OrderMessagePublisher(IRabbitMqPersistentConnection connection)
    {
        _connection = connection;
    }

    public Task PublishAsync(string orderJson)
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        using var channel = _connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: "orders",
            type: "direct",
            durable: true,
            autoDelete: false,
            arguments: null);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.ContentType = "application/json";

        var body = Encoding.UTF8.GetBytes(orderJson);

        channel.BasicPublish(
            exchange: "orders",
            routingKey: "order.created",
            mandatory: true,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }
}
```

要点：

- `CreateModel()` 前必须确保连接成功，否则会抛出 `InvalidOperationException`。
- `IModel` 不是线程安全对象，推荐每次发布创建短生命周期 channel，并用完释放。
- 如果希望 broker 重启后消息仍在，应设置 `DeliveryMode = 2`，并确保 exchange / queue 都是 durable。
- `mandatory: true` 可以在无法路由时触发 return 机制，但本库没有封装 `BasicReturn` 处理；需要调用方自行监听或使用上层模块。

---

## 发布端 channel 池 + publisher confirms

`RabbitMQ.Client` v6 的 `IModel`（channel）不是线程安全的，且"每条消息开/关一条 channel"会让 broker 端 channel 计数飙升、网络 RPC 噪音激增。本库提供 `IRabbitMqPublishChannelPool` 把这部分收敛到池里。

### 何时使用

- 高吞吐发布场景（毫秒级延迟 + 持续高 TPS）：用池。
- 一次性、低频发布：直接 `IRabbitMqPersistentConnection.CreateModel()` 也可以。

### 注册（典型）

```csharp
services.TryAddSingleton<IRabbitMqPublishChannelPool>(sp =>
    new RabbitMqPublishChannelPool(
        sp.GetRequiredService<IRabbitMqPersistentConnection>(),
        exchangeName: "myapp.events",
        maxSize: 8,
        logger: sp.GetRequiredService<ILogger<RabbitMqPublishChannelPool>>()));
```

`maxSize` 即同时持有 channel 的线程上限，由 `SemaphoreSlim` 限制；池外并发请求排队等待。

### 使用模式

```csharp
public sealed class OrderPublisher
{
    private readonly IRabbitMqPublishChannelPool _pool;

    public OrderPublisher(IRabbitMqPublishChannelPool pool) => _pool = pool;

    public void Publish(string routingKey, string json, Guid messageId)
    {
        using var rental = _pool.Acquire();          // ① 租用
        var channel = rental.Channel;

        var props = channel.CreateBasicProperties();
        props.DeliveryMode = 2;
        props.MessageId = messageId.ToString();
        channel.BasicPublish(
            exchange: "myapp.events",
            routingKey: routingKey,
            mandatory: true,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(json));

        rental.WaitForConfirmsOrThrow(TimeSpan.FromSeconds(5));  // ② 同步等回执
        // ③ using 退出时 Dispose 归还
    }
}
```

### 池里"每 channel 一次性"做的事

每次 `Acquire` 拿到的 channel 都已经预先完成：

- `ExchangeDeclare`（与池构造时的 `exchangeName` 一致，direct + durable）
- `ConfirmSelect`（开启 publisher confirms）
- 挂 `BasicReturn` 事件监听（mandatory:true 时退回路由）

调用方只需要 `BasicPublish` + `WaitForConfirmsOrThrow`，吞吐相比"每条消息开 channel"明显提升。

### `WaitForConfirmsOrThrow` 的语义

把 broker 三种失败信号统一转成 `RabbitMqPublishFailedException`：

| 异常 | 触发条件 |
|---|---|
| `RabbitMqPublishReturnedException` | mandatory:true + 无任何 queue 绑定到 routing key（broker 退回） |
| `RabbitMqPublishUnconfirmedException(timedOut: true)` | 超时未收到 broker 回执 |
| `RabbitMqPublishUnconfirmedException(timedOut: false)` | broker nack（未能持久化） |

> AMQP 协议保证 BasicReturn 帧先于 Ack 帧抵达，所以 `WaitForConfirms` 返回后立刻检查 `_pendingReturns` 是可靠的 —— 不会"先收 ack 再收 return"。

### 连接断开如何自愈

- `Acquire` 时取到的 channel 若已关闭（broker 重启 / 网络抖动），池里直接释放它并新建一条。
- `Return` 时检测 `IsOpen`，关闭的 channel 不再回收，槽位让池规模自然缩回。
- `IRabbitMqPersistentConnection` 自身会订阅 `ConnectionShutdown` / `CallbackException` 在后台线程重连，不会卡 channel pool 的调用方。

### 单测替换

接口设计成 `IRabbitMqPublishChannelPool` 就是为了在单测里能替换为 in-memory fake。`PooledChannel` 是 `readonly struct` 直接持有 channel 引用 + 池引用，fake 实现可在 `Acquire` 里返回任意 `IModel`，`Return` 走 fake 的回收逻辑。

---

## 快速开始：消费消息

消费入口是 `IRabbitMqMessageConsumerManager`。

```csharp
using System.Text;
using Core.RabbitMQ;

public sealed class RabbitMqConsumerHostedService : IHostedService
{
    private readonly IRabbitMqMessageConsumerManager _consumerManager;
    private IRabbitMqMessageConsumer _consumer;

    public RabbitMqConsumerHostedService(
        IRabbitMqMessageConsumerManager consumerManager)
    {
        _consumerManager = consumerManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer = _consumerManager.TryCreate(
            new RabbitMqExchangeDeclareConfigure(
                exchangeName: "orders",
                type: "direct",
                durable: true,
                autoDelete: false),
            new RabbitMqQueueDeclareConfigure(
                queueName: "order-service",
                durable: true,
                exclusive: false,
                autoDelete: false));

        await _consumer.BindAsync("order.created");

        _consumer.OnMessageReceived(async (model, eventArgs) =>
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            // 这里写业务处理逻辑。
            await HandleAsync(message, cancellationToken);

            // 不要在这里 BasicAck。默认 consumer 会在所有回调成功后自动 ack。
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Dispose();
        _consumerManager.TryRemove("orders", "order-service");
        return Task.CompletedTask;
    }

    private static Task HandleAsync(string message, CancellationToken cancellationToken)
    {
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
}
```

注册 HostedService：

```csharp
builder.Services.AddHostedService<RabbitMqConsumerHostedService>();
```

要点：

- `TryCreate(exchange, queue)` 会声明 exchange 和 queue。
- `BindAsync(routingKey)` 会执行 `QueueBind` 并记录绑定关系。
- `OnMessageReceived(...)` 注册消息处理回调。
- 默认实现会创建 `AsyncEventingBasicConsumer`，使用异步消费。
- 回调全部成功完成后，默认实现会执行 `BasicAck`。
- 回调中不要手动 `BasicAck` / `BasicNack`，否则可能与默认 ack 冲突。

---

## 消费失败策略 FailureBehavior 与 DLX

`DefaultRabbitMqMessageConsumer` 在所有回调跑完后会按 `RabbitMqOptions.FailureBehavior` 决定 ack/nack：

### 三种语义

```csharp
public enum RabbitMqFailureBehavior
{
    AlwaysAck = 0,
    NackNoRequeue = 1,
    RequeueOnce = 2,   // 默认
}
```

| 值 | 失败时行为 | 适用 |
|---|---|---|
| `RequeueOnce`（默认） | 首次失败 `nack(requeue=true)`；二次失败 `nack(requeue=false)`（用 `BasicDeliverEventArgs.Redelivered` 区分）。配合上层 inbox 去重达成"业务最终一致"，同时给 poison message 一个截断口 | 大多数业务 |
| `NackNoRequeue` | 失败立即 `nack(requeue=false)`：进 DLX 或被 broker 丢弃 | 可观测但接受丢失 |
| `AlwaysAck` | 失败也 ack —— 与旧版兼容；**没有 inbox 时会静默丢失** | 仅用于向后兼容 |

### 失败时谁负责截留 —— DLX

`RequeueOnce`/`NackNoRequeue` 在非重投路径会丢消息。如果没配 DLX，那些消息在 broker 端直接消失。

启用 DLX 由调用方在 queue 声明的 `arguments` 里加：

```csharp
new RabbitMqQueueDeclareConfigure(
    queueName: "order-service",
    arguments: new Dictionary<string, object>
    {
        ["x-dead-letter-exchange"] = "orders.dlx",
        ["x-dead-letter-routing-key"] = "order.failed"   // 可选；留空时复用原 routing key
    });
```

> 本库**不会**自动把 `RabbitMqOptions.DeadLetterExchange` 写入 queue 参数 —— 那是上层（如 `Core.EventBus.RabbitMQ.RabbitMqMessageSubscriber`）的职责，因为只有上层能决定每个 message type 对应哪个 queue。直接使用 `Core.RabbitMQ` 时，在调用 `TryCreate` 之前把上面 `arguments` 加好即可。

> 框架不代办 DLX → DLQ 的绑定。死信处置语义（归档 / 人工介入 / 转发其它系统）跟具体业务强相关，留给运维侧决定。

### 行为对照

| 场景 | AlwaysAck | NackNoRequeue | RequeueOnce |
|---|---|---|---|
| 单次失败 | ack（消息丢） | nack 不重投（进 DLX 或丢） | nack 重投，再来一次 |
| 二次失败 | ack（消息丢） | — | nack 不重投（进 DLX 或丢） |
| ack/nack 自身报错 | 仅 LogWarning，不再抛 | 同左 | 同左 |
| 多个 handler 中任一失败 | 余下 handler 不执行，按上面规则 ack/nack | 同左 | 同左 |

> "**ack/nack 自身报错**"在 channel 已关闭时会发生（broker 重启、网络抖动）；此时消息会被 broker 在 channel 重连后视为 unacked → 重投。这是预期行为。

---

## Exchange 与 Queue 声明

### RabbitMqExchangeDeclareConfigure

构造函数：

```csharp
public RabbitMqExchangeDeclareConfigure(
    string exchangeName,
    string type = "direct",
    bool durable = true,
    bool autoDelete = false,
    Dictionary<string, object> arguments = null)
```

默认值：

| 参数 | 默认值 |
|---|---|
| `type` | `"direct"` |
| `durable` | `true` |
| `autoDelete` | `false` |
| `arguments` | 空字典 |

调用 `Declare(IModel channel)` 时执行：

```csharp
channel.ExchangeDeclare(
    exchange: ExchangeName,
    type: Type,
    durable: Durable,
    autoDelete: AutoDelete,
    arguments: Arguments);
```

常见 exchange 类型：

```csharp
new RabbitMqExchangeDeclareConfigure("orders", "direct");
new RabbitMqExchangeDeclareConfigure("logs", "fanout");
new RabbitMqExchangeDeclareConfigure("topic-events", "topic");
```

### RabbitMqQueueDeclareConfigure

构造函数：

```csharp
public RabbitMqQueueDeclareConfigure(
    string queueName,
    bool durable = true,
    bool exclusive = false,
    bool autoDelete = false,
    Dictionary<string, object> arguments = null)
```

默认值：

| 参数 | 默认值 |
|---|---|
| `durable` | `true` |
| `exclusive` | `false` |
| `autoDelete` | `false` |
| `arguments` | 空字典 |

调用 `Declare(IModel channel)` 时执行：

```csharp
channel.QueueDeclare(
    queue: QueueName,
    durable: Durable,
    exclusive: Exclusive,
    autoDelete: AutoDelete,
    arguments: Arguments);
```

### 带死信参数的队列

本库不主动创建 DLX，但可以通过 `arguments` 声明队列参数：

```csharp
var queue = new RabbitMqQueueDeclareConfigure(
    queueName: "order-service",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: new Dictionary<string, object>
    {
        ["x-dead-letter-exchange"] = "orders.dlx",
        ["x-dead-letter-routing-key"] = "order.failed"
    });
```

---

## 连接管理实现细节

默认实现：`DefaultRabbitMqPersistentConnection`

### 连接创建

连接工厂由 `RabbitMqConnectionConfigure.ConnectionFactory` 创建：

```csharp
new ConnectionFactory
{
    HostName = HostName,
    Port = Port,
    UserName = UserName,
    Password = Password,
    DispatchConsumersAsync = true
};
```

如果 `VirtualHost` 非空，会设置到 `ConnectionFactory.VirtualHost`。

`DispatchConsumersAsync = true` 是异步 consumer 的必要配置，因为默认 consumer 使用 `AsyncEventingBasicConsumer`。

### 重试策略

`TryConnect()` 使用 Polly 对以下异常重试：

- `SocketException`
- `BrokerUnreachableException`

默认重试次数为 6 次，等待时间为指数退避：

```csharp
TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
```

即大致为 2s、4s、8s、16s、32s、64s。

### 连接事件

连接成功后会订阅 RabbitMQ.Client 事件：

| 事件 | 处理 |
|---|---|
| `ConnectionShutdown` | 后台线程跑 `TryConnect()` 重连。不会阻塞 client dispatcher 线程 |
| `CallbackException` | 同上 —— 后台线程重连 |
| `ConnectionBlocked` | **仅 `LogWarning`，不重连**。broker flow-control 时连接仍 open，重连只会绕弯路；等 broker 资源恢复 / `ConnectionUnblocked` 自动复活 |

> 重连不再在 RabbitMQ.Client 的 dispatcher 线程里同步等 Polly 几十秒退避，而是甩到线程池。否则会拖垮 broker 心跳与其它回调。

### Dispose 安全

- `Dispose()` 不持 `_syncRoot` 锁，而是直接把 `_connection` `Interlocked.Exchange` 出来释放。原因：`TryConnect()` 持锁时可能正在跑 Polly 退避（最坏约 126 秒）。`_disposed` 是 `volatile`，Polly 退避出来后会自检并清理 orphan 连接，整个流程不会卡 dispose。
- `TryConnect()` 入口立即检查 `_disposed`，dispose 之后再调用直接返回 false —— 防止"对象已 dispose、Polly 还在跑、之后又建出新连接"的 leak。

### CreateModel

```csharp
public IModel CreateModel()
```

如果当前没有可用连接，会抛出：

```text
No RabbitMQ connections are available to perform this action
```

因此推荐使用模式：

```csharp
if (!connection.IsConnected)
{
    connection.TryConnect();
}

using var channel = connection.CreateModel();
```

---

## 消费者实现细节

默认实现：`DefaultRabbitMqMessageConsumer`

### 创建流程

`IRabbitMqMessageConsumerManager.TryCreate(exchange, queue)` 会：

1. 用 `${exchangeName}_${queueName}` 作为 key 查找已有 consumer。
2. 如果已存在，直接返回同一个 consumer。
3. 如果不存在，创建 `DefaultRabbitMqMessageConsumer`。
4. 调用 `Initialize(exchange, queue)`。
5. `Initialize` 内部声明 exchange 和 queue。
6. 启动定时器维护消费 channel。

### 定时器行为

consumer 初始化后会创建 `Timer`：

- 首次延迟：2 秒
- 后续间隔：30 秒

定时器回调逻辑：

1. 如果 `ConsumerChannel` 存在且未关闭，直接返回。
2. 如果连接断开，调用 `TryConnect()`。
3. 创建新的 channel。
4. 创建 `AsyncEventingBasicConsumer`。
5. 设置 QoS：

```csharp
ConsumerChannel.BasicQos(0, 30, false);
```

6. 开始消费：

```csharp
ConsumerChannel.BasicConsume(
    queue: QueueDeclare.QueueName,
    autoAck: false,
    consumer: consumer);
```

### 消息回调

注册方式：

```csharp
consumer.OnMessageReceived(async (model, eventArgs) =>
{
    // 业务处理
});
```

实现内部用 `ConcurrentBag<Func<IModel, BasicDeliverEventArgs, Task>>` 保存回调。

重复注册判断基于 `processEvent.Method`：

```csharp
if (ProcessEvents.Any(p => p.Method == processEvent.Method))
{
    return;
}
```

因此同一个方法不会重复加入；但如果使用不同 lambda，即使逻辑相同，也可能被视为不同回调。

### Ack 语义

当前实现按 `RabbitMqOptions.FailureBehavior` 决定 ack/nack：

- `autoAck` 为 `false`，由本类按结果决定。
- 收到消息后按注册顺序逐个执行所有回调。**任一回调抛异常会跳出循环，后续回调不再执行。**
- 成功路径：`BasicAck(deliveryTag, multiple: false)`
- 失败路径：见 [消费失败策略 FailureBehavior 与 DLX](#消费失败策略-failurebehavior-与-dlx)。
- ack/nack 调用自身报错（典型场景：channel 已关闭）只 `LogWarning`，消息在 broker 端会因 unacked 在 channel 重连后自动重投 —— 不会让 PollLoop 崩溃。

要点：

- **不要在回调内手动 ack / nack**，会与默认逻辑冲突。
- **回调内吞掉业务异常 = 告诉框架"处理成功"**，会按成功路径 ack。要让重投/DLX 生效，把异常抛出去。
- 失败时调用方拿到 `(deliveryTag, routingKey, redelivered)` 三个上下文（通过日志），可用来排查 poison message。
- 失败次数控制 / 死信投递 / 重试退避：本库给的截断口是 `RequeueOnce`，更细的策略请：
  - 在上层用 inbox 表记失败次数；
  - 在 queue 声明里挂 DLX 参数；
  - 业务路径上跑单独的 retry exchange / retry queue（time-to-live + DLX 实现延迟队列）；
  - 或替换 `IRabbitMqMessageConsumer` 实现。

### Binding 管理

绑定：

```csharp
await consumer.BindAsync("order.created");
```

内部执行：

```csharp
channel.QueueBind(
    queue: QueueDeclare.QueueName,
    exchange: ExchangeDeclare.ExchangeName,
    routingKey: routingKey);
```

解绑：

```csharp
await consumer.UnbindAsync("order.created");
```

内部执行：

```csharp
channel.QueueUnbind(
    queue: QueueDeclare.QueueName,
    exchange: ExchangeDeclare.ExchangeName,
    routingKey: routingKey);
```

`HasAnyRoutingKey()` 用于判断当前 consumer 是否还记录着任何 routing key 绑定。

---

## 生命周期与释放

### IRabbitMqPersistentConnection

通过 DI 注册为 singleton，由容器释放。

`Dispose()` 会释放底层 RabbitMQ `IConnection`。

### IRabbitMqMessageConsumer

由 `IRabbitMqMessageConsumerManager` 创建和缓存。释放方式：

```csharp
// 直接交给 manager 兜底释放
consumerManager.TryRemove(exchangeName, queueName);
```

注意：

- `TryRemove` **会自动调用 `Dispose()`**（旧版本只移除字典、消费通道/定时器残留 → 资源泄漏，已修复）。
- `Dispose()` 流程：先停 timer（最多等 5 秒让排队回调跑完），再释放 `ConsumerChannel`，整段 dispose-safe，重复调用幂等。
- 如果调用方已经手动 `consumer.Dispose()` 了，再调 `TryRemove` 也安全（consumer 内部有 `_disposed` 守卫）。

`TryCreate` 也做了竞态保护：用 `try-get + try-add`，并发线程多建出的 consumer 实例会在丢失插入时被立刻 `Dispose`，避免后台 timer + channel 泄漏。

字典 key 改为 `(exchange, queue)` 元组，避免旧版 `$"{exchange}_{queue}"` 拼字符串的碰撞（`"a_b"+"c"` 与 `"a"+"b_c"` 会撞 key）。

### Scoped 服务使用

默认 consumer 回调只传入 `IModel` 和 `BasicDeliverEventArgs`，不会自动为每条消息创建 DI scope。

如果消息处理需要 scoped 服务，例如 `DbContext`，推荐注入 `IServiceScopeFactory`，在回调内部创建作用域：

```csharp
consumer.OnMessageReceived(async (model, eventArgs) =>
{
    using var scope = scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    // 使用 db 处理消息
    await db.SaveChangesAsync();
});
```

---

## 生产建议

### 发布端

- 每次发布使用短生命周期 `IModel`，不要跨线程共享 channel。
- 设置 `DeliveryMode = 2`，并声明 durable exchange / queue。
- 根据业务决定是否使用 `mandatory: true` 并处理 unroutable 消息。
- 对 publish 失败建立清晰策略：直接抛出、业务重试、outbox 持久化等。

### 消费端

- 业务处理尽量幂等。网络断开、channel 关闭、应用重启都可能导致消息再次投递。
- 不要在回调中手动 ack / nack。
- 对高价值消息，不建议只依赖当前默认失败语义；应配合 DLX、重试队列或上层事件总线。
- 回调内部如果捕获异常，需要明确决定是否仍然让默认 consumer ack。
- 需要 scoped 依赖时，手动创建 scope。

### 连接配置

- `HostName` 不要留空；集群节点用英文分号分隔。
- `VirtualHost` 为空时使用 RabbitMQ 默认虚拟主机。
- 生产环境不要使用默认 `guest/guest`。
- 确保 RabbitMQ 用户有对应 vhost 的 configure / write / read 权限。

### 日志

默认实现会记录：

- 尝试连接 RabbitMQ
- 连接失败重试
- 连接成功
- 连接 shutdown / blocked / callback exception 后重连
- consumer channel 创建 trace
- 消息处理异常

建议生产环境至少开启 warning / error 日志；排查连接与消费问题时临时开启 trace / debug。

---

## 常见问题

### 1. `CreateModel()` 抛出没有可用连接

异常：

```text
No RabbitMQ connections are available to perform this action
```

原因通常是：

- 没有先调用 `TryConnect()`。
- RabbitMQ 不可达。
- 配置错误，例如 host、port、vhost、账号密码不正确。

处理：

```csharp
if (!connection.IsConnected)
{
    var connected = connection.TryConnect();
    if (!connected)
    {
        throw new InvalidOperationException("RabbitMQ connect failed.");
    }
}
```

### 2. 消息消费后没有被 ack / 一直 unacked

当前实现按 `FailureBehavior` 决定 ack/nack：

- 成功 → ack；
- 失败 → 按策略 nack（默认 `RequeueOnce`：首次重投，二次不重投）；
- ack/nack 调用本身失败（通常 channel 已关闭）→ 只 LogWarning，消息保留 unacked，等待 broker 重投。

如果你看到消息卡在 unacked：

- 检查日志里是否有 `RabbitMQ ack/nack 调用失败` —— channel 已断开，broker 重连后会自动重投。
- 检查 handler 日志是否抛了异常 —— 这部分已经按 nack 路径处理，不应卡 unacked。
- 检查 broker 上 queue 的 consumer 是否还在线 —— 如果 consumer 死了，所有 unacked 会一直挂着。

### 3. 回调执行了多次

可能原因：

- 你注册了多个不同 lambda。
- 应用多实例部署，多个实例消费同一个 queue。
- 消息因连接断开 / nack 重投 / `FailureBehavior=RequeueOnce` 的首次失败重投。

处理：

- 保持消费处理幂等（用 inbox 表去重是最直接的办法）。
- 确认 `OnMessageReceived` 注册位置不会重复执行。
- 重复检查现在用 `Delegate.Equals`（比对 `Target + Method`），同一方法不会重复加入；不同实例的同方法也不会被误判为重复后被静默丢弃。

### 4. `TryRemove` 后还在消费

旧版本 `TryRemove` 只移除字典缓存，需要调用方手动 `consumer.Dispose()`。**现在 `TryRemove` 内部已经自动 `Dispose`**：

```csharp
consumerManager.TryRemove(exchangeName, queueName);   // 同时停 timer + 关 channel
```

如果你仍看到旧 consumer 在跑：

- 确认 RabbitMQ broker 上 queue 的 consumer 列表 —— 可能是其他实例。
- 检查日志：`RabbitMQ consumer dispose failed during TryRemove` 说明 Dispose 自身失败，可能 channel 已经断开，broker 重连后会自动清理。

### 5. 如何配置死信队列

用 `RabbitMqQueueDeclareConfigure.arguments` 传 RabbitMQ 队列参数：

```csharp
new RabbitMqQueueDeclareConfigure(
    "order-service",
    arguments: new Dictionary<string, object>
    {
        ["x-dead-letter-exchange"] = "orders.dlx",
        ["x-dead-letter-routing-key"] = "order.failed"
    });
```

同时需要声明对应 DLX exchange 和死信 queue。

### 6. 是否支持 topic / fanout

支持。`RabbitMqExchangeDeclareConfigure` 的 `type` 参数直接传给 RabbitMQ.Client：

```csharp
new RabbitMqExchangeDeclareConfigure("events", type: "topic");
new RabbitMqExchangeDeclareConfigure("broadcast", type: "fanout");
```

### 7. 是否支持 TLS、心跳、连接超时等高级参数

当前 `RabbitMqConnectionConfigure` 只暴露 HostName、Port、UserName、Password、VirtualHost，并在内部创建 `ConnectionFactory`。

如果需要 TLS、RequestedHeartbeat、AutomaticRecoveryEnabled、ClientProvidedName 等高级参数，有两种方式：

- 扩展 `RabbitMqConnectionConfigure`。
- 自定义注册 `IRabbitMqPersistentConnection`，并在 `AddRabbitMq` 前注册，利用 `TryAddSingleton` 避免被覆盖。

---

## 完整示例

### appsettings.json

```json
{
  "RabbitMq": {
    "Connection": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/"
    }
  }
}
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRabbitMq(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<OrderMessagePublisher>();
builder.Services.AddHostedService<OrderMessageConsumerHostedService>();

var app = builder.Build();

app.MapPost("/orders", async (
    OrderMessagePublisher publisher,
    CreateOrderRequest request) =>
{
    var json = System.Text.Json.JsonSerializer.Serialize(request);
    await publisher.PublishAsync(json);
    return Results.Accepted();
});

app.Run();

public sealed record CreateOrderRequest(string OrderId, decimal Amount);
```

### Publisher

```csharp
using System.Text;
using Core.RabbitMQ;
using RabbitMQ.Client;

public sealed class OrderMessagePublisher
{
    private readonly IRabbitMqPersistentConnection _connection;

    public OrderMessagePublisher(IRabbitMqPersistentConnection connection)
    {
        _connection = connection;
    }

    public Task PublishAsync(string json)
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        using var channel = _connection.CreateModel();

        var exchange = new RabbitMqExchangeDeclareConfigure("orders");
        exchange.Declare(channel);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: "orders",
            routingKey: "order.created",
            mandatory: true,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(json));

        return Task.CompletedTask;
    }
}
```

### Consumer

```csharp
using System.Text;
using Core.RabbitMQ;

public sealed class OrderMessageConsumerHostedService : IHostedService
{
    private readonly IRabbitMqMessageConsumerManager _consumerManager;
    private readonly ILogger<OrderMessageConsumerHostedService> _logger;
    private IRabbitMqMessageConsumer _consumer;

    public OrderMessageConsumerHostedService(
        IRabbitMqMessageConsumerManager consumerManager,
        ILogger<OrderMessageConsumerHostedService> logger)
    {
        _consumerManager = consumerManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer = _consumerManager.TryCreate(
            new RabbitMqExchangeDeclareConfigure("orders"),
            new RabbitMqQueueDeclareConfigure("order-service"));

        await _consumer.BindAsync("order.created");

        _consumer.OnMessageReceived(async (model, eventArgs) =>
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            _logger.LogInformation(
                "Received order message. RoutingKey={RoutingKey}, Body={Body}",
                eventArgs.RoutingKey,
                json);

            await Task.CompletedTask;
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Dispose();
        _consumerManager.TryRemove("orders", "order-service");
        return Task.CompletedTask;
    }
}
```

---

## 当前实现边界

当前类库刻意保持较薄的基础设施层，以下能力本库**已经内置**（相比旧版本）：

- ✅ Publisher confirms + channel 池（`IRabbitMqPublishChannelPool`）
- ✅ Unroutable message return → `RabbitMqPublishReturnedException`
- ✅ 失败 ack/nack 策略（`RabbitMqFailureBehavior.RequeueOnce` 默认）
- ✅ Channel 断开自愈（pool / consumer 都会重建并按记录的 routing key 重新绑定）

以下能力仍未内置：

- 消息序列化协议
- publisher / subscriber 高层抽象
- handler 自动发现与调度
- outbox / inbox
- 自动 DLX → DLQ 绑定（本库只在 `arguments` 里挂 `x-dead-letter-exchange`，绑定由运维侧完成）
- 自定义重试次数 / 退避算法（`RequeueOnce` 只给一次重投截断口）
- 延迟队列
- 每条消息自动创建 DI scope
- TLS 等高级连接参数配置（HostName/Port/UserName/Password/VirtualHost 之外的字段需要扩展 `RabbitMqConnectionConfigure`）

如果项目需要这些能力，优先考虑使用或扩展上层 `Core.EventBus.RabbitMQ`，或者在本库之上实现自己的业务封装。
