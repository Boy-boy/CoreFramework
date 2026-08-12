# 请求管道指南（Pipeline）

> 前置 → 处理 → 后置三段管道，配合优先级特性编排顺序。适合横切关注点（校验、审计、日志等）。

## 1. 引入

```csharp
// using Core.Modularity.Attribute;
// using Core.Pipeline;

[DependsOn(typeof(PipelineModule))]
public class StartupModule : CoreModuleBase
{
}
```

不走模块时：`services.AddPipeline(typeof(MyRequest).Assembly)`。

> ⚠️ `AddPipeline(params Assembly[])` **必须传程序集**——不传则扫描短路，不注册任何处理器。

## 2. 定义请求与处理器

```csharp
// using Core.Pipeline;

public class MyRequest : IRequest
{
    public IEnumerable<KeyValuePair<Type, object>> Features { get; set; }
}

public class MyHandler : IRequestHandler<MyRequest>
{
    public Task HandleAsync(MyRequest request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class MyPreHandler : IRequestPreHandler<MyRequest>
{
    public Task HandleAsync(MyRequest request, CancellationToken cancellationToken)
        => Task.CompletedTask;   // 前置：进入业务前执行
}

public class MyProHandler : IRequestProHandler<MyRequest>
{
    public Task HandleAsync(MyRequest request, CancellationToken cancellationToken)
        => Task.CompletedTask;   // 后置：业务完成后执行
}
```

## 3. 优先级

| 特性 | 作用 |
|---|---|
| `[PipelinePriority(int)]` | 控制三段管道顺序：前置（默认 `-999`）→ 处理（`998`）→ 后置（`999`） |
| `[HandlerPriority(int)]` | 控制同一段内多个处理器的执行顺序，越小越先 |

## 4. 调用

```csharp
// 注入 IPipelineProvider，获取某请求的管道委托后直接调用
var pipeline = provider.Get<MyRequest>();   // IPipelineProvider
await pipeline(new MyRequest(), CancellationToken.None);

// 或注入 IPipeline<MyRequest>，用 InvokeAsync 手动串联
```
