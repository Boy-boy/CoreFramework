# 核心框架使用手册

## 框架描述

该框架提供了一些基础功能的实现，比如ElasticSearch，Eventbus等。

## 框架使用描述

该框架提供了两种使用方式，一种直接依赖，另外一种则是模块化，接下来，我将介绍两种方式的使用

## 使用方式

### 模块化

#### 模块化

```c#
 public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationManager<StartupModule>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.InitializationApplicationBuilder();
        }
    }
```

```
    public class StartupModule : CoreModuleBase
    {
        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {    
        }

        public override void Configure(ApplicationBuilderContext context)
        {   
        }
    }
```

#### elasticSearch

```c#
 [DependsOn(      
        typeof(CoreElasticSearchModule))]
    public class StartupModule : CoreModuleBase
    {
        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {    
         //方式一
         context.services.Configure<ElasticClientFactoryOptions>(Configuration.GetSection("ElasticClient"));
         //方式二
          context.Services.AddElasticClientFactory("自定义名称"，options=>{
           options.UserName="";
           options.PassWord="";
           options.Urls=new string[]{};
           options.DefaultIndex="";
           options.ElasticClientLifeTime=TimeSpan.FromHours(24)//默认不小于1小时
          });
        }

        public override void Configure(ApplicationBuilderContext context)
        {   
        }
    }
```

```c#
  public interface IDemoEsRepository:IElasticSearchRepositories<自定义类型>
  {
     //自定义查询方法
  }

public class DemoEsRepository : ElasticSearchRepositories<自定义类型>, IDemoEsRepository
    {
        public ComputeApplyEsRepository(IElasticClientFactory elasticClientFactory)
        : base(elasticClientFactory,elasticClientName:"该名称须跟注入的名称匹配")
        {
        }
   }
```



### 直接依赖

#### elasticSearch

```
 //方式一
         services.Configure<ElasticClientFactoryOptions>(Configuration.GetSection("ElasticClient"));
         Services.AddElasticClientFactory();
 //方式二
         Services.AddElasticClientFactory("自定义名称"，options=>{
           options.UserName="";
           options.PassWord="";
           options.Urls=new string[]{};
           options.DefaultIndex="";
           options.ElasticClientLifeTime=TimeSpan.FromHours(24)//默认不小于1小时
          });
```

elasticSearch

#### eventbus

