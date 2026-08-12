# CoreFramework

CoreFramework（核心框架）是一个面向 .NET 的模块化应用开发框架。本词汇表定义框架文档、代码注释与协作时应使用的规范术语。

## Language

**Module**（模块）:
An independently assembled unit of functionality. An application is built by declaring a startup module and its dependencies, and the framework wires the whole graph.
_Avoid_: 插件（plugin）、组件（component）、类库（library）

**Modular**（模块化使用）:
The recommended way to use CoreFramework — compose capabilities by declaring modules rather than registering services one by one.
_Avoid_: 整包引入、模块系统

**Direct**（直接依赖）:
Using a single capability by calling its `AddXxx` extension on the service collection, skipping the module system.
_Avoid_: 普通引用、裸依赖

**EventBus**（事件总线）:
Publish–subscribe messaging infrastructure supporting in-process, RabbitMQ, and Kafka transports.
_Avoid_: 消息队列（MQ 只是其中一种传输层，不是总线本身）、Eventbus

**Domain Event**（领域事件）:
A business event collected on an aggregate root and published to the EventBus through the Outbox when the unit of work commits.
_Avoid_: 消息（泛指）、事件总线消息

**Unit of Work**（工作单元）:
A boundary that makes a group of repository operations commit atomically.
_Avoid_: 事务（transaction）、数据库事务

**Outbox**（出箱）:
Persistence of events waiting to be published, guaranteeing at-least-once delivery to the EventBus.
_Avoid_: 待发消息表

**Inbox**（入箱）:
Persistence of received events that makes consumption idempotent.
_Avoid_: 已收消息表

**DbConfiguration**（配置中心）:
A configuration source backed by a database, with a management dashboard.
_Avoid_: dbConfiguration、数据库配置、动态配置

**Scheduling**（调度）:
Framework-managed background or timed work, hosted on Quartz, Hangfire, or a BackgroundService.
_Avoid_: 定时任务（只指其中一类）、后台任务

**Sharding**（分表）:
Splitting a table's data across physical storage by a shard key.
_Avoid_: 分库、水平拆分

**Alert**（告警）:
The escalation engine that raises and routes alerts through levels.
_Avoid_: 通知（notification）

**Permission**（权限）:
RBAC-based access control enforced on HTTP endpoints.
_Avoid_: 授权（authorization）、鉴权、权限控制中间件
