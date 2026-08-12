# 权限指南（Permission）

> RBAC 接口权限控制：`[Permission]` 声明权限点，中间件按请求授权。**无模块类**，直接注册。

## 1. 注册

```csharp
// using Core.Permission;
// using Core.Permission.Storage;

builder.Services.AddPermission(o =>
    o.AddPostgreSql(x =>
    {
        x.DbConnection = "Host=localhost;Database=perms";
    }));

// 或内存存储：builder.Services.AddPermission(o => o.AddInMemory());

// app 构建后挂载中间件
app.UsePermission();
```

> ⚠️ **注意**：`AddPermission` 注册了 `IPermissionService` / `IPermissionHandler`，但**不会**注册处理器提供者。运行时授权前需补一行：
>
> ```csharp
> services.AddSingleton<IPermissionHandlerProvider, DefaultPermissionHandlerProvider>();
> ```

## 2. 声明权限点

```csharp
// using Core.Permission;
// using Microsoft.AspNetCore.Mvc;

[Permission("user.read")]                       // 控制器级：类上所有 action 生效
public class UserController : ControllerBase
{
    [Permission("user.create,user.update")]     // 逗号分隔 = 满足任一 policy 即通过
    public IActionResult Create() => Ok();

    public IActionResult Read() => Ok();        // 继承类级权限
}
```

## 3. 存储后端

| 存储 | 扩展方法 | 选项 |
|---|---|---|
| PostgreSQL | `o.AddPostgreSql(Action<PermissionPostgreSqlOptions>)` | `DbConnection` / `DbSchema`（默认 `Permission`）/ `DbTable`（默认 `AccessRole`） |
| 内存 | `o.AddInMemory()` | — |

## 4. 自定义授权逻辑

`IPermissionHandler` 提供默认角色实现，也可自定义：在 `HandlerAsync(PermissionHandlerContext)` 里判断后调用 `context.Succeed()` 或 `context.Fail()`。

```csharp
// using Core.Permission;

public class CustomHandler : IPermissionHandler
{
    public Task HandlerAsync(PermissionHandlerContext handlerContext)
    {
        // handlerContext.HttpContext / .Permissions
        return Task.CompletedTask;
    }
}
```
