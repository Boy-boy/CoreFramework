# 配置中心指南（DbConfiguration）

> 配置中心把配置放在数据库里，提供可视化 Dashboard 在线维护，并支持热生效。支持 SqlServer / PostgreSql / MySql 三种存储后端。

## 1. 构建阶段挂载数据库配置源

```csharp
// 使用 PostgreSql / MySql / SqlServer 对应的源
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddPostgreSqlConfigure(actionOptions =>
        {
            actionOptions.DbConnection = "Host=localhost;Port=5432;Database=customer;Username=postgres;Password=xxx";
        });
    })
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
```

三个扩展方法对应三种后端：`AddPostgreSqlConfigure` / `AddMySqlConfigure` / `AddSqlServerConfigure`（均在 `Microsoft.Extensions.Configuration` 命名空间）。

配置源选项（继承自 `DbConfigurationSource`）：

| 属性 | 缺省 | 说明 |
|---|---|---|
| `DbConnection` | — | 存储库连接串（必填） |
| `DbSchema` | `core` | 模式名 |
| `DbTable` | `sys_configuration` | 配置表名 |
| `Environment` | `Product` | 环境 |
| `NameSpace` | `all_server` | 命名空间 |
| `ReloadDelay` | — | 变更后重载延迟 |

## 2. 注册配置服务与 Dashboard

```csharp
// using Core.Configuration;
// using Core.Configuration.Dashboard;

[DependsOn(typeof(CoreConfigurationModule))]
public class StartupModule : CoreModuleBase
{
    public override void ConfigureServices(ServiceCollectionContext context)
    {
        context.Services.AddDbConfiguration(options =>
        {
            options.AddDashboard(actionOptions =>
            {
                actionOptions.PathMatch = "/config/dashboard";   // 默认 /config/dashboard
            });
        });
    }

    public override void Configure(ApplicationBuilderContext context)
    {
        context.ApplicationBuilder.UseEndpoints(endpoints =>
        {
            endpoints.MapDbConfigurationDashboard();
        });
    }
}
```

启动后访问 `PathMatch` 对应路径即可在线浏览 / 编辑配置项，改动通过变更通知自动重载到应用。

## 3. 更多

- `Core.Configuration`：数据库配置源 `IConfigurationProvider` + 变更通知
- `Core.Configuration.SqlServer` / `.PostgreSql` / `.MySql`：存储后端
- `Core.Configuration.Dashboard`：配置管理 Dashboard
