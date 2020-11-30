# 核心框架使用手册

- [核心框架使用手册](#核心框架使用手册)
  - [框架描述](#框架描述)
  - [框架使用描述](#框架使用描述)
  - [使用方式](#使用方式)
    - [模块化](#模块化)
      - [模块化](#模块化-1)
      - [elasticSearch](#elasticsearch)
      - [eventbus](#eventbus)
        - [RabbitMq](#rabbitmq)
      - [entityFraworkCore](#entityfraworkcore)
    - [直接依赖](#直接依赖)
      - [elasticSearch](#elasticsearch-1)
      - [eventbus](#eventbus-1)
        - [RabbitMq](#rabbitmq-1)

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

```c#
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
          context.Services.Configure<ElasticClientFactoryOptions>(Configuration.GetSection("ElasticClient"));
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

在appsetting.json配置Rabbitmq

 "ElasticClient": {
    "UserName": "elastic",
    "PassWord": "septnet",
    "Urls":[""]
  },

#### eventbus

##### RabbitMq

```c#
[DependsOn(      
        typeof(CoreEventBusRabbitMqModule))]
    public class StartupModule : CoreModuleBase
    {
        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void PreConfigureServices(ServiceCollectionContext context)
        {
         context.Services.Configure<EventBusOptions>(options =>
            {
             //若是订阅服务，添加自动扫描程序集，则自动注入Handler
                options.AutoRegistrarHandlersAssemblies = new[] { typeof(StartupModule).Assembly };
            });
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {    
         context.Services.Configure<EventBusRabbitMqOptions>(options =>
            {
               //配置发布交换器，可不配置
                options.AddPublishConfigure();
                //配置订阅交换器和队列，可不配置
                options.AddSubscribeConfigures();
            });
        }

        public override void Configure(ApplicationBuilderContext context)
        {   
        }
    }
```

2.在appsetting.json配置Rabbitmq

 "RabbitMq": {
    "Connection": {
      "hostName": "127.0.0.1",
      "userName": "guest",
      "password": "guest",
      "port": "-1",
      "virtualHost": "/"
    }
  }

#### entityFraworkCore

```c#
    [DependsOn(typeof(CoreEfCoreModule))]
    public class StartupModule : CoreModuleBase
    {
        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddControllers();
            
            //实现动态配置dbContext的信息，如：TableName，Schema等
            //若要实现动态配置，自定义的DbContext（CustomerDbContext）需继承CoreShardingDbContext
            context.Services.AddShardingDbContext<CustomerDbContext>(options =>
             {
                 options.UseSqlServer(Configuration.GetConnectionString("Customer"));
             });
        }

        public override void Configure(ApplicationBuilderContext context)
        {
        }
    }
```

描述：若想发送领域事件，自定义的DbContextCustomerDbContext需继承CoreDbContext，且添加自己的eventbus，如：

```
[DependsOn(      
        typeof(CoreEventBusRabbitMqModule))]
```



### 直接依赖

#### elasticSearch

```c#
   public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
 //方式一
         services.Configure<ElasticClientFactoryOptions>(Configuration.GetSection("ElasticClient"));
         Services.AddElasticClientFactory();
 //方式二
         services.AddElasticClientFactory("自定义名称"，options=>{
           options.UserName="";
           options.PassWord="";
           options.Urls=new string[]{};
           options.DefaultIndex="";
           options.ElasticClientLifeTime=TimeSpan.FromHours(24)//默认不小于1小时
          });
                  
        }
       
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
```

#### eventbus

##### RabbitMq

```c#
public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RabbitMqOptions>(Configuration.GetSection("RabbitMq"));
            context.Services.AddRabbitMq();
            
            context.Services
                .AddEventBus(options=>{
                 //若是订阅服务，添加自动扫描程序集，则自动注入Handler
                options.AutoRegistrarHandlersAssemblies = new[] { typeof(StartupModule).Assembly };
                })
                .AutoRegistrarHandlers(options.AutoRegistrarHandlersAssemblies);
           
            services.AddEventBusRabbitMq(options=>{
                //配置发布交换器，可不配置
                options.AddPublishConfigure();
                //配置订阅交换器和队列，可不配置
                options.AddSubscribeConfigures();
            });
            
            
        }
       
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
         //若是订阅服务，添加自动扫描程序集，则自动注入Handler
           app.UseEventBus();
        }
    }
```

