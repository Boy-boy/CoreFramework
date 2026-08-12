# ElasticSearch 指南

> 模块 `CoreElasticSearchModule` 提供 Elasticsearch 客户端工厂与仓储基类。默认绑定 `ElasticSearch` 配置节。

## 1. 引入模块

```csharp
// using Core.Modularity.Attribute;
// using Core.ElasticSearch;

[DependsOn(typeof(CoreElasticSearchModule))]
public class StartupModule : CoreModuleBase
{
    public override void ConfigureServices(ServiceCollectionContext context)
    {
        // 可选：代码注册命名客户端
        context.Services.AddElasticClientFactory("search", options =>
        {
            options.UserName = "elastic";
            options.PassWord = "xxx";                       // 注意是大写 W
            options.Urls = new[] { "http://localhost:9200" };
            options.DefaultIndex = "default_index";
            options.ElasticClientLifeTime = TimeSpan.FromHours(24); // 默认 24h，最短 1h
        });
    }
}
```

`AddElasticClientFactory` 有两个重载：

```csharp
services.AddElasticClientFactory();                                  // 默认客户端（走配置节）
services.AddElasticClientFactory("search", options => { ... });      // 命名客户端
```

## 2. 配置

`appsettings.json`：

```json
"ElasticSearch": {
  "UserName": "elastic",
  "PassWord": "xxx",
  "Urls": [ "http://localhost:9200" ],
  "DefaultIndex": "elastic_search_default_index"
}
```

选项对应 `ElasticClientFactoryOptions`（`Core.ElasticSearch.Options`）：

| 属性 | 说明 |
|---|---|
| `UserName` / `PassWord` | 认证（`PassWord` 大写 W） |
| `Urls` | 节点地址数组 |
| `DefaultIndex` | 默认索引（缺省 `elastic_search_default_index`） |
| `ElasticClientLifeTime` | 客户端缓存时长，默认 24h，最短 1h |

## 3. 仓储读写

```csharp
// using Core.ElasticSearch;

public interface IProductEsRepository : IElasticSearchRepositories<Product>
{
}

public class ProductEsRepository : ElasticSearchRepositories<Product>, IProductEsRepository
{
    public ProductEsRepository(IElasticClientFactory elasticClientFactory)
        : base(elasticClientFactory, elasticClientName: "search") // 名称须与 AddElasticClientFactory 一致
    {
    }
}
```

`ElasticSearchRepositories<T>` 构造第二个参数是客户端名称；缺省时用默认客户端。仓储基类内部封装了常见读写，也可在自定义仓储里通过 `IElasticClientFactory.CreateClient(name)` 直接拿到 `ElasticClient`（NEST）做精细操作。
