# 调度指南（Scheduling）

> 三种宿主（`BackgroundService` 默认 / Quartz / Hangfire）共享同一 `IScheduledHandler` 契约；Redis 分布式锁防止多实例重复执行。

## 1. 引入模块

```csharp
// using Core.Modularity.Attribute;

[DependsOn(typeof(SchedulingBackgroundModule))]   // 默认 BackgroundService 宿主
public class StartupModule : CoreModuleBase
{
}
```

可选宿主模块：

| 模块 | 说明 |
|---|---|
| `SchedulingBackgroundModule` | 默认 `BackgroundService` 宿主 |
| `SchedulingQuartzModule` | Quartz 适配（集群、cron、AdoJobStore） |
| `SchedulingHangfireModule` | Hangfire 适配（RecurringJob、Dashboard） |
| `SchedulingRedisModule` | Redis 分布式锁（叠加在 Background 宿主之上） |

## 2. 定义一个调度处理器

```csharp
// using Core.Scheduling;
// using Core.Scheduling.Abstractions;
// using Core.Scheduling.Models;

public sealed class MyJob : IScheduledHandler
{
    public string HandlerCode => "my-job";
    public string DisplayName => "My Job";
    public ScheduleDescriptor Schedule { get; } =
        ScheduleDescriptor.FixedInterval(TimeSpan.FromSeconds(5)); // 或 ScheduleDescriptor.Cron("0 0 * * * ?")

    public Task<HandlerExecutionResult> ExecuteAsync(HandlerExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(HandlerExecutionResult.Success(HandlerCode, context.FireTime, DateTimeOffset.UtcNow));
}
```

`ScheduleDescriptor` 无公开构造器，用静态工厂创建：

| 工厂 | 说明 |
|---|---|
| `ScheduleDescriptor.FixedInterval(TimeSpan, startDelay = null, allowConcurrentExecution = false, maxBackoff = null)` | 固定间隔 |
| `ScheduleDescriptor.Cron(string, timeZoneId = null, startDelay = null, allowConcurrentExecution = false, maxBackoff = null)` | Cron 表达式 |

## 3. 注册宿主与处理器

```csharp
// builder = WebApplication.CreateBuilder(args)，或任意 ServiceCollection
var services = builder.Services;
services.AddSchedulingBackground();      // 启动 BackgroundService 宿主
services.AddScheduledHandler<MyJob>();   // 注册处理器
```

多实例部署时追加 Redis 分布式锁：

```csharp
services.AddSchedulingRedisLock(options => options.ConnectionString = "localhost:6379");
```

## 4. 配置

`appsettings.json`（宿主各自绑定配置节）：

```json
"Scheduling": {
  "Background": {
    "DefaultMaxBackoff": "00:05:00",
    "IdleDelay": "00:00:00.500",
    "EnableDistributedLockRenewal": true
  },
  "Quartz": {
    "ClusterEnabled": true,
    "ThreadCount": 4
  },
  "Hangfire": {
    "PersistenceMode": "InMemory"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

## 5. 更多

- 过滤器（`AddSchedulingFilter<T>`）、健康检查（`AddSchedulingHealthCheck`）、完整配置项与集群说明 → `src/Core.Scheduling/README.md`
- 单实例不重复执行、失败重试与退避：见 `ScheduleDescriptor` 的 `maxBackoff` 参数与宿主选项
