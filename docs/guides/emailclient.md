# 邮件指南（EmailClient）

> MailKit 邮件发送 + 出箱式存储 + 后台发送。`SendAsync` 只是**入队**，实际 SMTP 发送由 `EmailClientBackgroundService` 完成，失败可重试。

## 1. 注册

```csharp
// using Core.EmailClient;
// using Core.EmailClient.PostgreSql;   // 或 Core.EmailClient.Mysql
// using Microsoft.Extensions.DependencyInjection;

services.AddEmailClient(o =>
{
    o.Host = "smtp.example.com";
    o.Port = 465;
    o.UseSsl = true;                // 默认 true
    o.ClientId = "user";
    o.ClientSecret = "pass";

    // 出箱式存储：已入队邮件先落库，后台服务逐个发送
    o.AddPostgreSql(x =>
    {
        x.DbConnection = "Host=localhost;Database=mail;Username=postgres;Password=xxx";
        x.DbTable = "emails";
    });
});
```

两种存储后端：

| 存储 | 扩展方法 | 选项 |
|---|---|---|
| PostgreSQL | `options.AddPostgreSql(Action<PostgreSqlEmailStorageOptions>)` | `DbConnection` / `DbSchema` / `DbTable` |
| MySQL | `options.AddMysql(Action<MysqlEmailStorageOptions>)` | `DbConnection` / `DbTable` |

## 2. 配置

`appsettings.json`（模块模式时 `EmailClient` 节，存储子节点 `EmailClient:Storage`）：

```json
"EmailClient": {
  "Host": "smtp.example.com",
  "Port": 465,
  "UseSsl": true,
  "ClientId": "user",
  "ClientSecret": "pass",
  "Storage": {
    "DbConnection": "Host=localhost;Database=mail;Username=postgres;Password=xxx",
    "DbTable": "emails"
  }
}
```

## 3. 发送

```csharp
// using Core.EmailClient;

var client = sp.GetRequiredService<IEmailClient>();
await client.SendAsync(new MailBodyEntity
{
    SenderAddress = "me@example.com",
    Recipients = new List<string> { "to@example.com" },
    Cc = new List<string> { "cc@example.com" },
    Subject = "Hello",
    Body = "<h1>Hi</h1>",
    BodyType = MailTextFormat.Html,
    MailFiles = new List<MailFile> { new("report.xlsx", fileBytes) }
});
```

`MailBodyEntity` 常用字段：`Sender` / `SenderAddress`、`Recipients` / `Cc` / `Bcc`、`Subject`、`Body`（`BodyType` 取 `MailTextFormat.Text|Html`）、`MailFiles`（附件）。

## 4. 更多

- 后台发送失败会保留在存储中，配合邮件表可观测与补偿。
- 可运行示例 → `test/Test`（`AddEmailClient(...)` 用法）。
