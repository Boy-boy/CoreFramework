# Core.Scheduling

> 统一调度运行时 —— 同一份业务代码,单机走 BackgroundService,集群任选 Redis 锁 / Quartz / Hangfire。

业务侧只见 `Core.Scheduling.Abstractions`(`IScheduledHandler` + `ScheduleDescriptor` + 几个结果类型),切换宿主**不动 handler 代码**。

---

## 目录

- [包结构](#包结构)
- [30 秒上手](#30-秒上手)
- [完整 Quickstart(端到端)](#完整-quickstart端到端)
- [集群方案怎么选](#集群方案怎么选)
- [配置详解](#配置详解)
- [Handler 编写契约](#handler-编写契约)
- [自定义过滤器](#自定义过滤器)
- [可观测性](#可观测性)
- [健康检查](#健康检查)
- [Inspector / 自查端点](#inspector--自查端点)
- [常见坑](#常见坑)
- [故意没做的事](#故意没做的事)

---

## 包结构

| 包 | 职责 | 你什么时候用 |
|---|---|---|
| `Core.Scheduling.Abstractions` | 公共契约:`IScheduledHandler`、`ScheduleDescriptor`、`HandlerExecution*`、`IHandlerExecutionFilter`、`IHandlerExecutionInspector`、可观测性常量 | 写业务 handler 时,**永远只引用它** |
| `Core.Scheduling` | 默认 BG 实现:`BackgroundService` 主循环 + 过滤器管线(Logging / Metrics / Tracing / 状态跟踪内置) | 单机、开发、CI、单元测试 |
| `Core.Scheduling.Redis` | BG 之上叠 Redis 分布式锁的**轻量集群**方案,无任务表、无迁移、无 Dashboard | 已有 Redis,想要"多节点同一时刻只一个跑"但不想引入 Quartz/Hangfire 体量 |
| `Core.Scheduling.Quartz` | Quartz.NET 适配器:AdoJobStore + `QRTZ_LOCKS` 集群仲裁;差量调度 + 孤儿清理 | 多节点生产 + 任意 `TimeSpan` 间隔 / Quartz Cron |
| `Core.Scheduling.Hangfire` | Hangfire 适配器:RecurringJob + 存储层分布式锁;Dashboard 直接可见 | 分钟级及以上节奏 + 要 Hangfire Dashboard / 运维已用 Hangfire |
| `Core.Scheduling.HealthChecks` | 聚合健康检查:把 `IHandlerExecutionInspector` 转成 `IHealthCheck`,按连续失败 / 卡死 / 陈旧三档判健康 | 任何环境,挂到 ASP.NET Core `/health` |

---

## 30 秒上手

```csharp
// 1. 业务 handler
public sealed class MyJob : IScheduledHandler
{
    public string HandlerCode  => "my-job";
    public string DisplayName  => "我的巡检";
    public ScheduleDescriptor Schedule { get; }
        = ScheduleDescriptor.FixedInterval(TimeSpan.FromSeconds(5));

    public Task<HandlerExecutionResult> ExecuteAsync(HandlerExecutionContext ctx, CancellationToken ct)
    {
        // 业务逻辑...
        return Task.FromResult(
            HandlerExecutionResult.Success(HandlerCode, ctx.FireTime, DateTimeOffset.UtcNow));
    }
}

// 2. 注册
services.AddSchedulingBackground();          // 默认 BG 宿主
services.AddScheduledHandler<MyJob>(); // 把 handler 加入注册表
```

启动就开跑。需要集群?往下看。

---

## 完整 Quickstart(端到端)

### 业务侧 — handler 的几种典型形态

**只读型(查库 / 推送 / 缓存预热)**

```csharp
public sealed class CacheWarmupJob : IScheduledHandler
{
    public string HandlerCode => "cache-warmup";
    public string DisplayName => "预热商品缓存";

    public ScheduleDescriptor Schedule { get; } = ScheduleDescriptor.FixedInterval(
        interval:   TimeSpan.FromMinutes(10),
        startDelay: TimeSpan.FromSeconds(30));   // 启动后等 30s 再首次执行,避开冷启动尖峰

    public async Task<HandlerExecutionResult> ExecuteAsync(
        HandlerExecutionContext ctx, CancellationToken ct)
    {
        // ctx.Services 是本次执行的全新 DI 作用域 —— scoped 服务直接拿
        var db    = ctx.Services.GetRequiredService<MyDbContext>();
        var cache = ctx.Services.GetRequiredService<IDistributedCache>();

        var products = await db.Products.AsNoTracking().ToListAsync(ct);
        await cache.SetStringAsync("hot-products", JsonSerializer.Serialize(products), ct);

        return HandlerExecutionResult.Success(
            HandlerCode, ctx.FireTime, DateTimeOffset.UtcNow,
            metrics: new Dictionary<string, long> { ["count"] = products.Count });
    }
}
```

**业务条件不满足 → 主动 Skipped(不算失败)**

```csharp
public async Task<HandlerExecutionResult> ExecuteAsync(HandlerExecutionContext ctx, CancellationToken ct)
{
    if (!_clock.IsTradingHours())
        return HandlerExecutionResult.Skipped(HandlerCode, ctx.FireTime, _clock.UtcNow,
            reason: "Outside trading hours.");
    // ...
}
```

**业务级失败(不抛异常的那种) → Failure**

```csharp
var result = await _upstream.PullAsync(ct);
if (!result.IsOk)
    return HandlerExecutionResult.Failure(
        HandlerCode, ctx.FireTime, DateTimeOffset.UtcNow,
        errorMessage: $"upstream returned {result.Code}");
```

> 抛异常也行,框架会兜底成 `Faulted`;但**能识别的失败建议返回 `Failure`** —— 语义清楚,不污染 trace。

**Cron 触发(仅 Quartz / Hangfire 模式)**

```csharp
public ScheduleDescriptor Schedule { get; } = ScheduleDescriptor.Cron(
    cronExpression: "0 0 2 * * ?",   // 每天凌晨 2 点(Quartz 6 字段;Hangfire 5 字段写 "0 2 * * *")
    timeZoneId:     "Asia/Shanghai",
    startDelay:     TimeSpan.Zero);
```

> BG 宿主对 Cron 描述符**启动期就抛 `NotSupportedException`**,强制切到 Quartz。

**允许并发执行**

```csharp
public ScheduleDescriptor Schedule { get; } = ScheduleDescriptor.FixedInterval(
    interval: TimeSpan.FromSeconds(1),
    allowConcurrentExecution: true);   // 默认 false —— 上一次没跑完就跳过本轮
```

---

### 注册方式 1:用 Core.Modularity 模块

```csharp
public class MyJobsModule : CoreModuleBase
{
    public override void ConfigureServices(ServiceCollectionContext context)
    {
        context.Services.AddScheduledHandler<CacheWarmupJob>();
        context.Services.AddScheduledHandler<ReconciliationJob>();
        // ...
    }
}

// CoreApplication 启动时挂模块即可:
//   modules.Add<SchedulingBackgroundModule>();              // 单机 BG
//   modules.Add<SchedulingHealthChecksModule>();  // 可选
//   modules.Add<MyJobsModule>();
```

`Core.Scheduling` 提供 4 个内置模块,二选一(再加 HealthChecks):

| 模块 | 等价 | 何时挂 |
|---|---|---|
| `SchedulingBackgroundModule` | `AddSchedulingBackground()` | 单机 BG |
| `SchedulingQuartzModule` | `AddSchedulingQuartz(...)` | Quartz 集群 |
| `SchedulingHangfireModule` | `AddSchedulingHangfire(...)` | Hangfire 集群 |
| `SchedulingRedisModule` | `AddSchedulingRedisLock(...)` | 叠在 BG 上做 Redis 锁仲裁 |
| `SchedulingHealthChecksModule` | `AddSchedulingHealthCheck(...)` | 加聚合健康检查 |

模块从 `appsettings.json` 的 `Scheduling`、`Scheduling:Quartz`、`Scheduling:Hangfire`、`Scheduling:Redis`、`Scheduling:HealthChecks` 节读配置。

### 注册方式 2:直接 `IServiceCollection` 扩展(不依赖 Core.Modularity)

```csharp
// 单机 —— 只设 BG 字段(filter 开关默认全开)
services.AddSchedulingBackground(o =>
{
    o.IdleDelay = TimeSpan.FromMilliseconds(500);
});

// 想关掉某个 filter 再传第二个回调
services.AddSchedulingBackground(
    o => o.IdleDelay = TimeSpan.FromMilliseconds(500),
    filters => filters.EnableMetrics = false);

// 单机 + Redis 锁
services.AddSchedulingBackground(o => o.DistributedLockLeaseDuration = TimeSpan.FromSeconds(30));
services.AddSchedulingRedisLock(o =>
{
    o.ConnectionString = "127.0.0.1:6379";
    o.KeyPrefix        = "core-scheduling:lock:";
});

// Quartz 集群 —— scheduling 回调可省,3 个 filter 开关默认全开
services.AddSchedulingQuartz(quartz =>
{
    quartz.PersistenceMode  = QuartzPersistenceMode.SqlServer;
    quartz.ConnectionString = "Server=...;Database=Scheduler;Trusted_Connection=True;";
    quartz.ClusterEnabled   = true;
    quartz.ThreadCount      = 4;
});

// 想关掉某个 filter 再传第二个参数
services.AddSchedulingQuartz(
    quartz =>
    {
        quartz.PersistenceMode  = QuartzPersistenceMode.SqlServer;
        quartz.ConnectionString = "Server=...;Database=Scheduler;Trusted_Connection=True;";
    },
    scheduling => scheduling.EnableMetrics = false);

// Hangfire 集群 —— 同样的 pattern,configureHangfire 必填,scheduling 可省
services.AddSchedulingHangfire(hangfire =>
{
    hangfire.PersistenceMode  = HangfirePersistenceMode.SqlServer;
    hangfire.ConnectionString = "Server=...;Database=Scheduler;Trusted_Connection=True;";
    hangfire.PollingInterval  = TimeSpan.FromSeconds(15);
});

// 加 handler(三种宿主一致)
services.AddScheduledHandler<CacheWarmupJob>();
services.AddScheduledHandler<ReconciliationJob>();
```

> **三个适配器 API 对称**:每个 Add 扩展第一个回调配宿主专属字段(BG / Hangfire / Quartz),
> 第二个回调配 **`SchedulingFilterOptions`**(共享的 3 个 filter 开关),都可省。
> Hangfire / Quartz 模式下,`IdleDelay` / 分布式锁 / `DefaultMaxBackoff` 等 BG 专属字段
> 在类型层就拿不到 —— 强制把"在那个宿主下没意义的配置项"拦在外面。
> 这两类配置分两个独立 Options 实例,在 DI 里各自注入 / 热更新。

---

## 集群方案怎么选

| 方案 | 加什么 | 触发粒度 | 外部依赖 | 选它的理由 |
|---|---|---|---|---|
| **BG + Redis 锁** | `SchedulingBackgroundModule` + `SchedulingRedisModule` | 任意 `TimeSpan` | 一个 Redis | 已有 Redis,要轻量 / 秒级 |
| **Quartz** | `SchedulingQuartzModule` | 任意 `TimeSpan` + Cron | SqlServer / Postgres + 11 张 QRTZ_ 表 | 要 Cron 又要任意间隔,不想引 Redis |
| **Hangfire** | `SchedulingHangfireModule` | 分钟级 + Cron(5 字段) | SqlServer + Hangfire 表 | 要 Dashboard;团队已熟悉 Hangfire |

**Hangfire 不能秒级**:`Schedule.FixedInterval(< 60s)` 在启动期会抛 `InvalidOperationException` 提示切到 BG / Quartz。

---

## 配置详解

### 基础 `Scheduling` 节

同一个 JSON 节同时绑两个 Options 类型:
- **`SchedulingFilterOptions`** —— 3 个共享 filter 开关,三种宿主都生效
- **`SchedulingOptions`** —— BG 专属字段(`IdleDelay`、停机、退避、分布式锁),仅 BG 模式下读

```json
{
  "Scheduling": {
    "IdleDelay": "00:00:00.500",
    "ShutdownGraceTimeout": "00:00:30",
    "DefaultMaxBackoff": "00:05:00",
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": true,
    "DistributedLockLeaseDuration": "00:00:30",
    "EnableDistributedLockRenewal": true,
    "DistributedLockRenewalFraction": 0.5
  }
}
```

| 字段 | 默认 | 归属类型 | 说明 | BG | Hangfire | Quartz |
|---|---|---|---|:-:|:-:|:-:|
| `EnableTracing` | true | `SchedulingFilterOptions` | OTel/Activity span | ✅ | ✅ | ✅ |
| `EnableMetrics` | true | `SchedulingFilterOptions` | Counter + Histogram | ✅ | ✅ | ✅ |
| `EnableLogging` | true | `SchedulingFilterOptions` | 结构化日志 | ✅ | ✅ | ✅ |
| `IdleDelay` | 500 ms | `SchedulingOptions` | 主循环空闲轮询间隔 | ✅ | ❌ | ❌ |
| `ShutdownGraceTimeout` | 30 s | `SchedulingOptions` | 停机等待 in-flight handler 上限 | ✅ | ❌ | ❌ |
| `DefaultMaxBackoff` | 5 min | `SchedulingOptions` | 失败退避封顶(`Schedule.MaxBackoff` 优先) | ✅ | ❌ | ❌ |
| `DistributedLock*` | — | `SchedulingOptions` | 仅 BG + 真实锁(如 Redis)生效 | ✅ | ❌ | ❌ |

> Hangfire / Quartz 模式下 BG-only 字段(SchedulingOptions)根本不会被绑定,
> JSON 里写了也无影响。三个 filter 开关由 `SchedulingCoreModule` 统一绑,
> 各适配器共用。

### Quartz 专属 `Scheduling:Quartz`

```json
{
  "Scheduling": {
    "Quartz": {
      "PersistenceMode": "SqlServer",
      "ConnectionString": "Server=...;Database=Scheduler;Trusted_Connection=True;",
      "SchedulerName": "Core.Scheduling.Scheduler",
      "InstanceId": "AUTO",
      "TablePrefix": "QRTZ_",
      "ClusterEnabled": true,
      "ClusterCheckinInterval": "00:00:10",
      "ThreadCount": 4,
      "JobGroup": "core-scheduling",
      "CleanupOrphanJobs": true
    }
  }
}
```

部署前要执行建表脚本(随包发布):
- `sql/tables_sqlserver.sql`
- `sql/tables_postgres.sql`

### Hangfire 专属 `Scheduling:Hangfire`

```json
{
  "Scheduling": {
    "Hangfire": {
      "PersistenceMode": "SqlServer",
      "ConnectionString": "Server=...;Database=Scheduler;Trusted_Connection=True;",
      "SqlServerSchemaName": "HangFire",
      "PrepareSchemaIfNecessary": true,
      "PollingInterval": "00:00:15",
      "JobIdPrefix": "core-scheduling:",
      "WorkerCount": 8,
      "CleanupOrphanJobs": true,
      "NonConcurrentLockTimeoutSeconds": 300
    }
  }
}
```

`PrepareSchemaIfNecessary=true` 时 Hangfire 首次启动自建表(便利但有写 schema 权限);生产建议关掉,手动跑官方 migration。

### Redis 专属 `Scheduling:Redis`

```json
{
  "Scheduling": {
    "Redis": {
      "ConnectionString": "127.0.0.1:6379",
      "KeyPrefix": "core-scheduling:lock:",
      "Database": 0
    }
  }
}
```

`KeyPrefix` 在多套应用共用同一 Redis 时用来隔离。`ConnectionString` 为空时本扩展不建 `IConnectionMultiplexer`,期待外部已经注册(常见于已有 `Core.Redis` 的项目)。

### HealthChecks 专属 `Scheduling:HealthChecks`

```json
{
  "Scheduling": {
    "HealthChecks": {
      "UnhealthyAfterConsecutiveFailures": 5,
      "DegradedAfterConsecutiveFailures": 2,
      "RunningThresholdForDegraded": "00:05:00",
      "StaleThresholdForDegraded": null,
      "IncludePerHandlerDetails": true,
      "IncludedHandlerCodes": [],
      "ExcludedHandlerCodes": [ "noisy-debug-job" ]
    }
  }
}
```

> **集群下不要开 `StaleThresholdForDegraded`** —— 别的节点抢的触发不会更新本节点 `LastFinishTime`,会误报陈旧。

---

## Handler 编写契约

### 结果状态 → 框架动作

| 返回 / 抛 | 状态 | 失败计数 | 下次时间(BG) |
|---|---|---|---|
| `HandlerExecutionResult.Success(...)` | Success | 重置为 0 | `now + Interval` |
| `HandlerExecutionResult.Skipped(...)` | Skipped | 不动 | `now + Interval` |
| `HandlerExecutionResult.Failure(...)` | Failure | +1 | `now + min(Interval × 2^(n-1), MaxBackoff)` |
| `HandlerExecutionResult.Cancelled(...)` | Cancelled | 不动 | `now + Interval` |
| 抛 `OperationCanceledException`(因 ct) | Cancelled(框架兜底) | 不动 | 同上 |
| 抛其它异常 | Faulted(框架兜底) | +1 | 同 Failure |

> **Hangfire / Quartz 模式下**框架不算下次时间(由 RecurringJob/Trigger 引擎决定),`HandlerState.NextRunTime` 保持 `null`。失败计数仍正常累计,仍能驱动健康检查。

### handler 生命周期

- 注册为**单例**(`services.AddScheduledHandler<T>` 内部就是 `AddSingleton`)
- **构造期**做轻量初始化即可,不要做 I/O
- **每次执行**由框架建一个全新的 DI 作用域 `ctx.Services`,scoped 服务从这里取,执行结束自动 dispose
- handler 自己别捕 `OperationCanceledException` 然后继续干 —— 让它冒出来,框架会标 `Cancelled`

### scoped 服务示例(DbContext)

```csharp
public async Task<HandlerExecutionResult> ExecuteAsync(HandlerExecutionContext ctx, CancellationToken ct)
{
    // 直接 GetRequiredService —— ctx.Services 已经是新 scope
    var db = ctx.Services.GetRequiredService<MyDbContext>();
    var count = await db.Orders.Where(o => o.Status == "pending").CountAsync(ct);
    return HandlerExecutionResult.Success(HandlerCode, ctx.FireTime, DateTimeOffset.UtcNow,
        metrics: new Dictionary<string, long> { ["pending"] = count });
}
```

需要嵌套作用域(如背景循环里多次开 scope)再 `ctx.Services.CreateScope()`;一般用例直接拿就行。

---

## 自定义过滤器

过滤器是 ASP.NET Core middleware 式的"洋葱链",`Order` 升序从外到内套。框架内置 4 个:

| Filter | Order | 作用 |
|---|---|---|
| `TracingExecutionFilter` | 10 | OTel span |
| `LoggingExecutionFilter` | 100 | 结构化日志 |
| `MetricsExecutionFilter` | 200 | Counter + Histogram |
| `StateTrackingFilter` | 1000 | 状态记录(最内层,所有异常在这里兜底) |

写一个重试过滤器:

```csharp
public sealed class RetryFilter : IHandlerExecutionFilter
{
    public int Order => 500;   // 在 Metrics(200) 之内、StateTracking(1000) 之外

    public async Task<HandlerExecutionResult> InvokeAsync(
        HandlerExecutionContext ctx,
        HandlerExecutionDelegate next,
        CancellationToken ct)
    {
        HandlerExecutionResult last = null!;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            last = await next(ctx, ct).ConfigureAwait(false);
            if (last.IsSuccess || last.Status == HandlerExecutionStatus.Cancelled)
                return last;
            if (last.Status == HandlerExecutionStatus.Skipped)
                return last;
            // Failure / Faulted 才重试
            await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
        }
        return last;
    }
}

services.AddSchedulingFilter<RetryFilter>();
```

Order 推荐区间:

- **10–99** 全局观测层(Tracing 类)
- **100–199** 日志层
- **200–299** 指标层
- **300–999** 业务横切(重试、限流、熔断、租户切换、运行时开关)
- **1000+** 框架级状态(保留)

外层异常会被 `StateTrackingFilter` 兜底,但**自己 filter 抛出异常会冒到框架**,Quartz 模式下会被适配器重抛回 Quartz 触发其 misfire / failure 计数。

---

## 可观测性

### Metrics(`System.Diagnostics.Metrics`)

```csharp
// Program.cs / Startup
services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddMeter("Core.Scheduling")          // 订阅本框架 Meter
        .AddPrometheusExporter());
```

| 指标 | 类型 | 维度 |
|---|---|---|
| `scheduling.executions.count` | Counter | `handler.code`,`status` |
| `scheduling.executions.duration` | Histogram(ms) | `handler.code`,`status` |

### Tracing(`System.Diagnostics.ActivitySource`)

```csharp
services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource("Core.Scheduling")
        .AddOtlpExporter());
```

每次 handler 执行一个 span,name = `scheduling.execute {handler.code}`,
标签含 `handler.code` / `status` / `scheduling.scheduled_time_ms`(Unix ms long)。

### 关掉某一类

```json
{ "Scheduling": { "EnableMetrics": false } }
```

---

## 健康检查

### 模块化

```csharp
modules.Add<SchedulingHealthChecksModule>();   // 默认绑 Scheduling:HealthChecks 配置节
```

然后在 ASP.NET Core 里照常 `app.MapHealthChecks("/health");` 即可。

### 手工

```csharp
services.AddSchedulingHealthCheck(opts =>
{
    opts.UnhealthyAfterConsecutiveFailures = 5;
    opts.DegradedAfterConsecutiveFailures  = 2;
    opts.RunningThresholdForDegraded       = TimeSpan.FromMinutes(5);
    opts.IncludePerHandlerDetails          = true;
    // 集群下不开 StaleThresholdForDegraded
});

app.MapHealthChecks("/health");
```

### 判定优先级(每个 handler 各自打分,取最严重)

1. `Unhealthy` — 连续失败 ≥ `UnhealthyAfterConsecutiveFailures`
2. `Degraded` — 卡死(running 时长 > `RunningThresholdForDegraded`)
3. `Degraded` — 陈旧(LastFinish 距今 > `StaleThresholdForDegraded`)
4. `Degraded` — 连续失败 ≥ `DegradedAfterConsecutiveFailures`
5. `Healthy`

聚合结果 = 所有参与 handler 中最严重者。

### 输出示例(`IncludePerHandlerDetails=true`)

```json
{
  "status": "Degraded",
  "description": "cache-warmup: 2 consecutive failures (>= 2).",
  "data": {
    "handlerCount": 3,
    "runningCount": 0,
    "checkedAt": "2026-06-25T12:00:00+00:00",
    "handler:cache-warmup": {
      "isRunning": false,
      "consecutiveFailures": 2,
      "lastStatus": "Failure",
      "lastStart": "2026-06-25T11:59:50+00:00",
      "lastFinish": "2026-06-25T11:59:55+00:00",
      "lastSuccess": "2026-06-25T11:50:00+00:00",
      "nextRun": "2026-06-25T12:00:10+00:00",
      "lastError": "upstream returned 503"
    }
  }
}
```

> Hangfire / Quartz 模式下 `nextRun` 字段为 `null` —— 由它们的 Dashboard / `QRTZ_TRIGGERS` 才能看到真实下次时间。

---

## Inspector / 自查端点

`IHandlerExecutionInspector` 是个只读视图,任意位置注入即可。健康检查就是基于它实现的;你也可以自己暴露一个调试端点:

```csharp
app.MapGet("/scheduling/status", (IHandlerExecutionInspector inspector) =>
{
    var snapshot = inspector.GetAllStates();
    return Results.Json(snapshot.Select(s => new
    {
        s.HandlerCode,
        s.IsRunning,
        s.LastStatus,
        s.LastStartTime,
        s.LastFinishTime,
        s.LastSuccessTime,
        s.NextRunTime,
        s.ConsecutiveFailureCount,
        s.LastError
    }));
});

app.MapGet("/scheduling/status/{code}", (string code, IHandlerExecutionInspector inspector) =>
{
    var state = inspector.GetState(code);
    return state is null ? Results.NotFound() : Results.Json(state);
});
```

视图**只反映本节点**:集群下别的节点的执行不会出现在这里。要全集群视图请查 `QRTZ_FIRED_TRIGGERS` 或 Hangfire Dashboard。

---

## 常见坑

### 1. Hangfire Cron 字段数与 Quartz 不一样

| 引擎 | 5 字段 | 6 字段 | 7 字段 |
|---|---|---|---|
| Hangfire(Cronos) | 分钟级 | 秒级 | ❌ |
| Quartz | ❌ | 秒级 | 含年份 |

写好的 Cron 表达式**不能跨适配器照搬**。

### 2. `IdleDelay` vs `PollingInterval` 不是一回事

- BG 的 `IdleDelay` 是**主循环空转间隔**,直接影响最小触发抖动。
- Hangfire 的 `PollingInterval` 是 Hangfire 服务器去存储**轮询 RecurringJob 的间隔**,与本框架的 IdleDelay 无关 —— 实际触发节奏取 cron。

### 3. Quartz `ClusterCheckinInterval` 设太短

Quartz 集群在该间隔内没看到节点心跳就视为掉线。设 1s 类的过分小值会触发**误判 + 集体抢锁风暴**。默认 10s 是经验值,改前先看官方文档。

### 4. Hangfire 适配器秒级直接抛

```csharp
public ScheduleDescriptor Schedule { get; }
    = ScheduleDescriptor.FixedInterval(TimeSpan.FromSeconds(5));   // ⚠️
```

挂 `SchedulingHangfireModule` 后这个 handler **启动期就崩**,异常里会提示切到 BG / Quartz。

### 5. `HandlerState.NextRunTime` 在 Hangfire/Quartz 下是 null

不是 bug。这两种模式下真实触发时间由引擎决定;框架不重算以免和引擎不一致。**要看下次时间请查 Hangfire Dashboard 或 `QRTZ_TRIGGERS.NEXT_FIRE_TIME`**。

### 6. handler 抛异常和返回 `Failure` 在度量上不一样

- 抛异常 → `Faulted`,trace 有 exception,Logging filter 打 Error
- 返回 `Failure` → `Failure`,trace 没有 exception,Logging filter 打 Warning

两者都 +1 失败计数,但前者噪声大。**能用 Failure 就别让异常冒出来**。

### 7. Redis 锁租约太短会被自己续租跟不上

`DistributedLockLeaseDuration` 必须 > 最长可能执行时间。
框架按 `DistributedLockRenewalFraction`(默认 0.5)每半个租约自动续一次;
租约设 5s 但 handler 跑 10s,Redis 心跳来不及续就会被别人抢锁导致重叠执行。
**租约保守地设 30s+**,handler 真的需要更长就调更大。

### 8. handler 单例 + 字段就有共享状态

```csharp
public sealed class BadJob : IScheduledHandler
{
    private List<int> _seen = new();   // ⚠️ 跨次执行被复用,且默认 false 时不会并发但 AllowConcurrent=true 会爆
    // ...
}
```

handler 是单例,字段在所有触发之间共享。要每次干净状态请在 `ExecuteAsync` 内部局部 new。

---

## 故意没做的事

- **跨节点状态可见**:`IHandlerExecutionInspector` 只看本节点。集群整体视图查 `QRTZ_FIRED_TRIGGERS` / Hangfire Dashboard。
- **handler 动态新增/卸载**:bootstrap 期一次性扫描;新增 handler 重启或重载模块。
- **BG 模式下的 Cron**:启动期抛 `NotSupportedException`,强制切到 Quartz。
- **Dashboard / 管理 UI**:暂不内置。Quartz 查表,Hangfire 用官方 Dashboard。
- **Hangfire / Quartz 模式下框架算 NextRunTime**:刻意不算,以免与引擎实际触发时间脱节;`HandlerState.NextRunTime` 在这两种模式下保持 `null`。
- **handler 内直接拿分布式锁续租**:`IDistributedHandlerLockHandle` 暂未对 handler 暴露,框架自己续。需要应用层细粒度续租自行扩展 filter。
