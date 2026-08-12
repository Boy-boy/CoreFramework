# EF Core + 工作单元 + 领域事件指南

> 模块 `CoreEfCoreModule` 提供 EF Core 集成：`CoreDbContext`、仓储自动注册、工作单元(UoW) 原子提交，以及"聚合根领域事件随 SaveChanges 自动发布到事件总线"的链路。

## 1. 引入模块

```csharp
// using Core.Modularity.Attribute;
// using Core.EntityFrameworkCore;

[DependsOn(
    typeof(CoreEfCoreModule),
    typeof(CoreEventBusRabbitMqModule),
    typeof(CoreEventBusEfCoreStorageModule))]   // 领域事件需要事件总线 + 出箱存储
public class StartupModule : CoreModuleBase
{
    public override void PreConfigureServices(ServiceCollectionContext context)
    {
        // 向模块声明 DbContext，CoreEfCoreModule 会自动为其注册仓储
        context.Items.Add(nameof(CustomerDbContext), typeof(CustomerDbContext));
    }

    public override void ConfigureServices(ServiceCollectionContext context)
    {
        context.Services.AddDbContext<CustomerDbContext>(options =>
        {
            options.UseSqlServer(Configuration.GetConnectionString("Customer"));
        });
    }
}
```

## 2. 不走模块、一次性注册

```csharp
// using Microsoft.EntityFrameworkCore;
// using Core.EntityFrameworkCore;

services.AddDbContextAndEfRepositories<CustomerDbContext>(options =>
{
    options.UseSqlServer(Configuration.GetConnectionString("Customer"));
});
```

等价地：`AddDbContext<TDbContext>(...)` 之后再 `services.AddRepositories<TDbContext>()`。

## 3. 领域事件

让 DbContext 继承 `CoreDbContext`。提交 `SaveChanges` 时，框架把受跟踪聚合根上的领域事件收集进当前工作单元，由 `OutboxDispatcher` 发布到事件总线——业务侧只需在聚合根上收集事件，无需手动调用总线：

```csharp
// using Core.Ddd.Domain.Entities;
// using Core.EventBus;

public class Order : AggregateRoot<long>
{
    public void Confirm()
    {
        AddDistributedEvent(new OrderConfirmed());   // 随 SaveChanges 原子发布
    }
}

public class OrderConfirmed : Message   // Core.EventBus.Message
{
}
```

聚合根还提供 `AddLocalEvent(Message)`（进程内领域事件）。领域事件随事务提交原子发布，失败可回滚。

## 4. 工作单元

仓储不做 `SaveChanges`，由工作单元统一提交。注册 `app.UseUnitOfWork()`（或使用 `[UnitOfWork]` 特性）后，一次请求内的操作在同一事务边界内原子提交：

```csharp
// Startup.Configure 中
app.UseUnitOfWork();

// 业务代码中可显式控制
var uow = unitOfWorkManager.Begin();
await uow.CommitAsync();
```

- `Core.Uow`：`IUnitOfWorkManager`、`IUnitOfWorkAccessor`、`[UnitOfWork]`、`UnitOfWorkMiddleware`
- `Core.Ddd.Domain`：`Entity`、`AggregateRoot<TKey>`、`ValueObject`、`IRepository<TEntity,TKey>`、`IDomainService`、`ISoftDeleted`

## 5. 更多

- 分表支持 → `Core.EntityFrameworkCore.Sharding`（`AddShardingDbContext<T>`、`CoreShardingDbContext`）
- 可运行示例 → `test/EntityFrameworkCore.Api`
