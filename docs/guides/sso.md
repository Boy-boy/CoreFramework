# 第三方 SSO 指南

> 第三方 OAuth / OIDC 单点登录。模块 `CoreThirdPartyAuthenticationModule` 注册认证方案，并在登录 / 登出路径上挂载端点。

## 1. 引入模块

```csharp
// using Core.Modularity.Attribute;
// using Core.Authentication.ThirdParty.Sso;

[DependsOn(typeof(CoreThirdPartyAuthenticationModule))]
public class StartupModule : CoreModuleBase
{
}
```

模块会自动完成两件事，无需手动调用：

- `AddThirdPartyAuthentication(configuration)` ——读取 `ThirdPartyAuthentication` 配置节注册认证方案
- `UseThirdPartyAuthentication()` ——挂载 OAuth 端点

等价的手工注册（不走模块时）：

```csharp
// using Core.Authentication.ThirdParty.Sso.Oauth;

services.AddThirdPartyAuthentication(Configuration.GetSection("ThirdPartyAuthentication"));
// app 构建后
app.UseThirdPartyAuthentication();
```

## 2. 配置

`appsettings.json` 的 `ThirdPartyAuthentication` 节，`Schemes` 是按方案名组织的字典：

```json
"ThirdPartyAuthentication": {
  "DefaultScheme": "YXSTOauth",
  "PathBase": "/thirdParty/oauth",
  "SignInPath": "/signIn",
  "SignOutPath": "/signOut",
  "Schemes": {
    "YXSTOauth": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "SignInScheme": "ThirdPartyCookie",
      "AuthorizationEndpoint": "https://idp.example.com/oauth2/authorize",
      "TokenEndpoint": "https://idp.example.com/oauth2/token",
      "EndSessionEndpoint": "https://idp.example.com/oauth2/logout",
      "SignedInRedirectUri": "https://app.example.com/signin-oauth",
      "SignedOutRedirectUri": "https://app.example.com/signout-oauth",
      "CallbackPath": "/signin-callback",
      "RemoteSignOutPath": "/signout-callback"
    }
  }
}
```

- 所有 OAuth 端点挂在 `PathBase`（默认 `/thirdParty/oauth`）之下。
- `SignInScheme` 使用框架内置的 `ThirdPartyCookie`。

## 3. 更多

- 完整配置项说明 → `src/Core.Authentication.ThirdParty.Sso/README.md`
- 可运行示例 → `test/ThirdPartySso.WebApi`（Minimal API 风格引导）
