## 配置文件(demo)
 "ThirdPartyAuthentication": {
    "DefaultScheme": "YXSTOauth",     //默认sso方案
    "PathBase": "/thirdParty/oauth",  //路由基路径，配置好不可轻易修改
    "SignInPath": "/signIn",          //登录路径，配置好不可轻易修改
    "SignOutPath": "/signOut",        //登出路径，配置好不可轻易修改
    "CustomPrivateIps":"",            //配置内网IP
    "RealClientUriScheme":"",        //真实客户端scheme，默认http协议可不配置，若为https，即填写https
    "RealClientUriHost":"",           //真实客户端host，可不配置，默认程序能获取真实的host
    "RealClientUriPort":31000,        //真实客户端Port,必须配置（因网关修改，导致无法获取真实的端口信息）
    "RealClientUriPathBase":"/api/openapiService/thirdParty/oauth",//真实客户端base地址，必须配置（因网关修改，导致无法获取真实的地址信息）。
    "Schemes": {
      "YXSTOauth": {
        "ClientId": "",                      //客户端Id
        "ClientSecret": "",                  //客户端密钥
        "SignInScheme": "ThirdPartyCookie",  //保存登录状态介质（默认cookie scheme,ThirdPartyCookie默认值，不可修改）
        "SignOutScheme": "ThirdPartyCookie", //删除登录状态介质（默认cookie scheme,ThirdPartyCookie默认值，不可修改）
        "AuthorizationEndpoint": "",         //认证终结点
        "TokenEndpoint": "",                 //Token终结点
        "EndSessionEndpoint": "",            //登出终结点
        "SignedInRedirectUri": "/api/basicService/customtokengrant/redirectto", //登录成功后重定向地址
        "SignedOutRedirectUri": "/signIn",                                      //登出后重定向地址，默认跳转到登录路径
        "CallbackPath": "/signin-callback",                                     //登录成功后，第三方sso重定向地址
        "RemoteSignOutPath": "/signout-callback"                                //远程登出后回调地址
      }
    }
  }