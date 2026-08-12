# 模块化使用指南

> 推荐的使用方式：用模块声明依赖，框架按拓扑自动装配，编排完整生命周期。

## 模块系统

- **模块**：继承 `CoreModuleBase` 的类，是一个自包含的能力单元。应用由"一个启动模块 + 一串被依赖的模块"组成。
- **依赖**：`[DependsOn(typeof(SomeModule), typeof(OtherModule))]` 声明模块依赖，允许重复使用；框架做拓扑排序并检测循环依赖。
- **生命周期**：每个模块依次执行
  `PreConfigureServices → ConfigureServices → PostConfigureServices`（DI 阶段，参数 `ServiceCollectionContext`），
  再执行 `PreConfigure → Configure → PostConfigure`（应用阶段，参数 `ApplicationBuilderContext`），
  关闭时按逆序执行 `Shutdown(ShutdownApplicationContext)`。所有钩子都是虚方法，按需重写。
- **上下文传递**：`ServiceCollectionContext.Services` 是服务集合，`.Items` 是一个 `IDictionary<string, object>` 袋，用于模块间传递声明（例如向 `CoreEfCoreModule` 声明 DbContext）。

## 引导启动

只需两行代码（扩展方法在 `Core.Modularity` 命名空间）：

- `services.ConfigureServiceCollection<TModule>()`（约束 `T : ICoreModule`）——从启动模块出发，反射收集全部可达模块，按 `[DependsOn]` 拓扑排序，执行 DI 阶段。
- `app.BuildApplicationBuilder()` ——执行应用阶段，并在宿主停止时触发 `Shutdown`。

### 经典 `Startup` 风格

```csharp
// Program.cs
Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
    .Build()
    .Run();

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureServiceCollection<StartupModule>();   // DI 阶段
    }

    public void Configure(IApplicationBuilder app)
    {
        app.BuildApplicationBuilder();                          // 应用阶段
    }
}
```

### Minimal API 风格

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureServiceCollection<StartupModule>();
var app = builder.Build();
app.BuildApplicationBuilder();
app.Run();
```

## 启动模块示例

```csharp
// using Core.Modularity;
// using Core.Modularity.Attribute;
// using Core.EventBus;

[DependsOn(typeof(CoreEventBusModule))]
public class StartupModule : CoreModuleBase
{
    public StartupModule(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public override void ConfigureServices(ServiceCollectionContext context)
    {
        // 在此注册服务；依赖的模块已按拓扑顺序先执行完
    }

    public override void Configure(ApplicationBuilderContext context)
    {
        // 在此装配管道（中间件、端点等）
    }
}
```

> 依赖的模块在被声明前不需要手动实例化——框架会按依赖顺序自动创建、装配并执行各自的生命周期钩子。若依赖模块需要配置项，它会在自己的 `ConfigureServices` 里绑定配置节，无需你关心。
