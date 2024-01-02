namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public class ThirdPartyAuthenticationOptions
    {
        /// <summary>
        /// sso方案
        /// </summary>
        public string DefaultScheme { get; set; }

        /// <summary>
        /// 路由基路径，配置好不可轻易修改
        /// </summary>
        public string PathBase { get; set; } = "/thirdParty/oauth";

        /// <summary>
        /// 登录路径，配置好不可轻易修改
        /// </summary>
        public string SignInPath { get; set; } = "/signIn";

        /// <summary>
        /// 登出路径，配置好不可轻易修改
        /// </summary>
        public string SignOutPath { get; set; } = "/signOut";

        /// <summary>
        /// 自定义内网ip，多个用逗号分割
        /// </summary>
        public string CustomPrivateIps { get; set; }

        /// <summary>
        /// 真实客户端Scheme（http/https）
        /// </summary>
        public string RealClientUriScheme { get; set; }

        /// <summary>
        /// 真实客户端Host
        /// </summary>
        public string RealClientUriHost { get; set; }

        /// <summary>
        /// 真实客户端Port
        /// </summary>
        public int? RealClientUriPort { get; set; }

        /// <summary>
        /// 真实客户端基路径
        /// </summary>
        public string RealClientUriPathBase { get; set; }
    }
}
