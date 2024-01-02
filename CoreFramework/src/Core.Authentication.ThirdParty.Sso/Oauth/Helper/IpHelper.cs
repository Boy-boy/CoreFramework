using System.Net;

namespace Core.Authentication.ThirdParty.Sso.Oauth.Helper
{
    public class IpHelper
    {
        /// <summary>
        /// 自定义内网Ip事件
        /// </summary>
        public static event Action<List<string>> CustomPrivateIpsEvent;

        /// <summary>
        /// 验证是否是内网ip
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static bool IsPrivateIp(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out var ip) && IsPrivateIp(ip);
        }

        /// <summary>
        /// 验证是否是内网ip
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static bool IsPrivateIp(IPAddress ipAddress)
        {
            var customPrivateIps = new List<string>();
            CustomPrivateIpsEvent?.Invoke(customPrivateIps);
            if (customPrivateIps.Contains(ipAddress.ToString()))
            {
                return true;
            }

            var addressBytes = ipAddress.GetAddressBytes();
            switch (ipAddress.AddressFamily)
            {
                // IP地址为IPv4
                // A类私有地址：10.0.0.0 - 10.255.255.255
                case System.Net.Sockets.AddressFamily.InterNetwork when addressBytes[0] == 10:
                // B类私有地址：172.16.0.0 - 172.31.255.255
                case System.Net.Sockets.AddressFamily.InterNetwork when addressBytes[0] == 172 && addressBytes[1] >= 16 && addressBytes[1] <= 31:
                // C类私有地址：192.168.0.0 - 192.168.255.255
                case System.Net.Sockets.AddressFamily.InterNetwork when addressBytes[0] == 192 && addressBytes[1] == 168:
                    return true;
                // 零地址（0.0.0.0）和回送地址（127.0.0.1）算作私有地址
                case System.Net.Sockets.AddressFamily.InterNetwork when addressBytes[0] == 0 || addressBytes[0] == 127:
                    return false;
                // IP地址为IPv6
                // IPv6的私有地址前缀为fd00::/8
                case System.Net.Sockets.AddressFamily.InterNetworkV6 when addressBytes[0] == 0xfd:
                    return true;
                default:
                    return false;
            }
        }
    }
}
