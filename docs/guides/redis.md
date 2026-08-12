# Redis 缓存指南

> 模块 `CoreRedisModule` 提供 Redis 缓存封装：`IRedisCache`（字符串 / 列表 / 哈希 / 集合 / 有序集合 / Stream）+ JSON 扩展 + 分布式锁。

## 1. 引入模块

```csharp
// using Core.Modularity.Attribute;
// using Core.Redis;

[DependsOn(typeof(CoreRedisModule))]
public class StartupModule : CoreModuleBase
{
}
```

不走模块时：`services.AddRedisCache(options => options.Configuration = "localhost:6379");`

## 2. 配置

`appsettings.json`（模块绑定 `Redis` 节 → `RedisCacheOptions`）：

```json
"Redis": {
  "Configuration": "localhost:6379"
}
```

常用选项：

| 属性 | 缺省 | 说明 |
|---|---|---|
| `Configuration` | — | 连接串（如 `localhost:6379,abortConnect=false`） |
| `ConfigurationOptions` | — | StackExchange.Redis 完整配置；若设置则优先于 `Configuration` |
| `InstancePrefix` | — | key 前缀 |
| `DefaultDatabase` | `-1` | 默认库 |

## 3. 使用

```csharp
// using Core.Redis;

public class CartService
{
    private readonly IRedisCache _cache;
    public CartService(IRedisCache cache) => _cache = cache;

    public async Task SetAsync(Cart cart, CancellationToken ct)
        => await _cache.SetJsonAsync("cart:" + cart.Id, cart, TimeSpan.FromMinutes(30), cancellationToken: ct);

    public async Task<Cart?> GetAsync(string id, CancellationToken ct)
        => await _cache.GetJsonAsync<Cart>("cart:" + id, cancellationToken: ct);
}
```

`IRedisCache` 能力一览：

| 类别 | 方法（异步对应 +Async） |
|---|---|
| 键值 | `Get` / `Set` / `SetExpireTime` / `Exists` / `Remove` |
| JSON | `SetJson<T>` / `GetJson<T>`（扩展方法，内部 System.Text.Json） |
| 计数 | `Increment` / `Decrement` |
| 分布式锁 | `TryAcquireLockAsync` / `ReleaseLock` / `ReleaseLockAsync` |
| 列表 / 哈希 / 集合 / 有序集合 / Stream | `ListLeftPush` / `HashGet` / `SetAdd` / `SortedSetAdd` / `StreamAdd` 等 |

## 4. 更多

- 分布式锁用于多实例互斥的典型场景：`TryAcquireLockAsync(key, clientId, expiry)` 成功再执行业务，`finally` 中 `ReleaseLock(key, clientId)`。
- 调度模块的多实例去重也可交给 `Core.Scheduling.Redis`（见 [调度指南](scheduling.md)）。
