# HttpClient 指南

> 类型化 HTTP 客户端 `IBaseHttpClient`，基于 `IHttpClientFactory`；可选 Kerberos 认证。

## 1. 注册

```csharp
// using Core.HttpClient;

services.AddBaseHttpClient();
// 或 services.AddBaseHttpClient(httpClient => httpClient.Timeout = TimeSpan.FromSeconds(30));
```

注册 `IBaseHttpClient → BaseHttpClient`，可直接注入。

## 2. 使用

```csharp
// using Core.HttpClient;

var client = sp.GetRequiredService<IBaseHttpClient>();

HttpClientResponse resp = await client.GetAsync(
    "https://api.example.com/data",
    new Dictionary<string, string> { ["X-Api-Key"] = "k" },
    CancellationToken.None);

if (resp.Error is null)
{
    Console.WriteLine(resp.Result);   // 响应字符串
}
else
{
    // 请求失败，resp.Error 携带异常
}
```

`IBaseHttpClient` 方法一览：

| 方法 | 说明 |
|---|---|
| `GetAsync(string url, Dictionary<string,string> requestHeaders, CancellationToken)` | GET |
| `PostAsync(string url, string content, CancellationToken)` | POST（原始字符串 body） |
| `SendAsync(HttpRequestMessage requestMessage, CancellationToken)` | 自定义完整请求 |

返回值 `HttpClientResponse`：`Result`（响应字符串）、`Error`（异常，null 表示成功）。

## 3. Kerberos 认证

```csharp
// using Core.HttpClient.Kerberos;

services.AddKerberosAuthentication(Configuration.GetSection("Kerberos"));
// 或 services.AddKerberosAuthentication(options => { options.Principal = "user@REALM"; options.KeytabPath = "/etc/krb5.keytab"; });
// 或 services.AddKerberosHttpClient("kerberos-client", "https://kerberos-service.example.com");
```

`KerberosOptions` 配置节 `Kerberos`：`Principal`、`KeytabBase64` / `KeytabPath`（默认 `/etc/krb5.keytab`）、`Realm`、`Kdc`、`DefaultServiceSpn`、票据相关选项。
