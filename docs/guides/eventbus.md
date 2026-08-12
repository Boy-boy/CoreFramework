# 事件总线指南（EventBus）

> 事件总线统一发布 / 订阅语义，RabbitMQ 与 Kafka 只是传输实现。出箱(Outbox) + 入箱(Inbox) 保证可靠投递与消费幂等。

## 1. 引入模块

以 RabbitMQ 传输 + EF Core 出箱存储为例：

```csharp
// using Core.Modularity.Attribute;
// using Core.EventBus;
// using Core.EventBus.RabbitMQ;
// using Core.EventBus.Storage.EfCore;

[DependsOn(
    typeof(CoreEventBusRabbitMqModule),
    typeof(CoreEventBusEfCoreStorageModule))]
public class StartupModule : CoreModuleBase
{
    public override void ConfigureServices(ServiceCollectionContext context)
    {
        // 订阅端：注册消费者程序集（按 [MessageName] 路由，可跨程序集）
        context.Services.Configure<EventBusOptions>(options =>
        {
            options.AddConsumers(typeof(Startup).Assembly);
        });
    }
}
```

可选模块：

| 模块 | 作用 |
|---|---|
| `CoreEventBusModule` | 事件总线契约 + 进程内总线 |
| `CoreEventBusLocalModule` | 进程内（本地）事件订阅 |
| `CoreEventBusRabbitMqModule` | RabbitMQ 传输 |
| `CoreEventBusKafkaModule` | Kafka 传输 |
| `CoreEventBusEfCoreStorageModule` | 出箱 / 入箱 / 死信表的 EF Core 存储 |

## 2. 定义消息与处理器

```csharp
// using Core.EventBus;

[MessageName("order.created")]        // 消息名，发布与订阅须一致；缺省为类名
public class OrderCreated : Message
{
}

[MessageHandlerPriority(1)]           // 同一消息可被多个处理器订阅，数值越小越先执行；默认 0
public class OrderCreatedHandler : IMessageHandler<OrderCreated>
{
    // 每次消费从 DI scope 解析，可注入 Scoped 服务；不应持有跨调用可变状态
    public Task HandleAsync(OrderCreated message, CancellationToken cancellationToken = default)
    {
        // 业务处理
        return Task.CompletedTask;
    }
}
```

属性一览：

| 属性 | 作用 |
|---|---|
| `[MessageName("name")]` | 消息名（类上），发布与订阅保持一致；缺省为类名 |
| `[MessageGroup("group")]` | 消息所属组（类上），缺省为服务名 |
| `[MessageHandlerPriority(n)]` | 同消息多处理器执行顺序（类/方法上），默认 0 |

## 3. 发布消息

```csharp
// using Core.EventBus;
// using Core.EventBus.Integration;

public class OrderService
{
    private readonly IIntegrationPublisher _publisher;
    public OrderService(IIntegrationPublisher publisher) => _publisher = publisher;

    public Task CreateAsync(CancellationToken ct)
        => _publisher.PublishAsync(new OrderCreated(), ct);   // 跨进程传输
}
```

进程内事件则注入 `ILocalPublisher`（`Core.EventBus.Local`）。两者都继承自统一契约 `IMessagePublisher`。

## 4. RabbitMQ 配置

`appsettings.json`（broker 子节点 `EventBus:RabbitMq:Broker` 对应 `RabbitMqOptions`）：

```json
"EventBus": {
  "RabbitMq": {
    "ExchangeName": "exchange_name",
    "Broker": {
      "Connection": {
        "hostName": "localhost",
        "userName": "guest",
        "password": "guest",
        "port": 5672,
        "virtualHost": "/"
      },
      "ConsumerPrefetchCount": 30,
      "FailureBehavior": "RequeueOnce"
    }
  }
}
```

也可在代码里配置：

```csharp
options.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));
// 或 options.AddRabbitMq(rabbitOptions => { rabbitOptions.ExchangeName = "..."; });
```

## 5. 更多

- 出箱 / 入箱 / 死信存储、Kafka 传输、错误与重试策略的完整说明 → `src/Core.EventBus/README.md`
- RabbitMQ 底层基础设施（持久连接、消费者管理、发布确认）→ `src/Core.RabbitMQ/README.md`
- Kafka 底层基础设施（幂等生产者、消费者管理）→ `src/Core.Kafka/README.md`
- 可运行示例 → `test/eventBus/PublishApi` 与 `test/eventBus/SubscriptionApi`
