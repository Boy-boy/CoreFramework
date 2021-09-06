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
      - [dbConfiguration](#dbConfiguration)
    - [直接依赖](#直接依赖)
      - [elasticSearch](#elasticsearch-1)
      - [eventbus](#eventbus-1)
        - [RabbitMq](#rabbitmq-1)
      - [entityFraworkCore](#entityfraworkcore-1)
      - [dbConfiguration](#dbConfiguration-1)

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
            services.ConfigureServiceCollection<StartupModule>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.BuildApplicationBuilder();
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
      : base(elasticClientFactory,elasticClientName:"该名称须跟注入的名称匹配   ")
      {
      }
  }
```
```json
在appsetting.json配置ElasticClient

 "ElasticClient": {
    "UserName": "elastic",
    "PassWord": "septnet",
    "Urls":[""],
    "DefaultIndex":"elastic_search_default_index"
  },
```

#### eventbus

##### RabbitMq

```c#
[DependsOn(      
         typeof(CoreEventBusRabbitMqModule),
         typeof(CoreEventBusSqlServerModule))]
    public class StartupModule : CoreModuleBase
    {
        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
      
        public override void ConfigureServices(ServiceCollectionContext context)
        {    

            var rabbitMqConnection = Configuration.GetSection("RabbitMq:Connection").Get<RabbitMqConnectionConfigure>();
            context.Services.Configure<EventBusRabbitMqOptions>(options =>
            {
                options.RabbitMqConnection = rabbitMqConnection;
            });

            //若事件需持久化，则需依赖CoreEventBusSqlServerModule
            context.Services.Configure<EventBusPostgreSqlOptions>(options =>
            {
                options.DbConnection = Configuration.GetConnectionString("customer");
            });
                                             
            //若该服务是订阅服务，则需配置以下代码
            context.Services.ConfigureEventBusOptions(options =>
            {
                options.AutoRegistrarHandlersAssemblies = new[] { typeof(StartupModule).Assembly };
            });    
        }
    }
```
```json
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
```

注意：<font color='red'> 1.发布和订阅消息，需定义消息名称，且要保持一致，请使用MessageNameAttribute</font>
     2.可配置事件处理器生命周期，请使用MessageHandlerLifetimeAttribute，默认是Transient（仅可定义在class上）
     3.同一个消息可被多个处理器订阅，可配置处理器处理顺序，请使用MessageHandlerPriorityAttribute
     4.可配置消息所属组，需在消息上使用MessageGroupAttribute（默认为服务名称）
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
        
        public override void PreConfigureServices(ServiceCollectionContext context)
        {
            context.Items.Add(nameof(CustomerDbContext), typeof(CustomerDbContext));     
        }
        
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddControllers();
                    
            context.Services.AddDbContext<CustomerDbContext>(options =>
             {
                 options.UseSqlServer(Configuration.GetConnectionString("Customer"));
             });
        }
    }
```

描述：若想发送领域事件，自定义的DbContext需继承CoreDbContext，且添加自己的eventbus，如：

```
[DependsOn(      
        typeof(CoreEventBusRabbitMqModule),
         typeof(CoreEventBusSqlServerModule))]
```

#### dbConfiguration
```c#
 public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    //添加数据库配置文件，例如PostgreSql，Mysql，SqlServer
                    builder.AddPostgreSqlConfigure(actionOptions =>
                    {
                        actionOptions.DbConnection = "Host=**;Port=5432;Database=customer;Username=postgres;Password=123456";
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
```
```c#
    [DependsOn(typeof(CoreConfigurationModule))]
    public class StartupModule : CoreModuleBase
    {
        public StartupModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public override void PreConfigureServices(ServiceCollectionContext context)
        {
        }
        
        public override void ConfigureServices(ServiceCollectionContext context)
        {       
            context.Services.AddDbConfiguration(options =>
            {
                options.AddDashboard(actionOptions =>
                 {
                   //请求/config/dashboard 即可跳转到db config配置页面,可配置，如下
                   actionOptions.PathMatch = "/config/dashboard";
                 });
            }); 
        }

        public override void Configure(ApplicationBuilderContext context)
        {
            var app = context.ApplicationBuilder;
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDbConfigurationDashboard();              
            });
        }
    }
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
            //services.AddEventBus(options =>
            //{
            //    options.AutoRegistrarHandlersAssemblies = new[] { typeof(Startup).Assembly };
            //    options.AddRabbitMq(rabbitOptions =>
            //    {
            //        rabbitOptions.ExchangeName = "demo";
            //        rabbitOptions.RabbitMqConnection = new RabbitMqConnectionConfigure();
            //    });
            //});
        }      
    }
```

#### entityFraworkCore

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
          services.AddDbContextAndEfRepositories<CustomerDbContext>(options =>
          {
              options.UseInMemoryDatabase("customer");
          }) .AddUnitOfWork();
          
          //方式二
           services.AddDbContext<CustomerDbContext>(options =>
          {
              options.UseInMemoryDatabase("customer");
          })
          .AddEfRepositories<CustomerDbContext>()
          .AddUnitOfWork();
        }   
    }
```

#### dbConfiguration
```c#
 public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    //添加数据库配置文件，例如PostgreSql，Mysql，SqlServer
                    builder.AddPostgreSqlConfigure(actionOptions =>
                    {
                        actionOptions.DbConnection = "Host=**;Port=5432;Database=customer;Username=postgres;Password=123456";
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
```

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
            services.AddDbConfiguration(options =>
            {
               options.AddDashboard(actionOptions =>
                 {
                   //请求/config/dashboard 即可跳转到db config配置页面,可配置，如下
                   actionOptions.PathMatch = "/config/dashboard";
                 });
            }); 
        }   

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDbConfigurationDashboard();
            });
        }
    }
```

