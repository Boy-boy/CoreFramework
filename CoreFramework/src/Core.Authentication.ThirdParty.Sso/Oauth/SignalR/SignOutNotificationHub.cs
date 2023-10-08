using Microsoft.AspNetCore.SignalR;

namespace Core.Authentication.ThirdParty.Sso.Oauth.SignalR
{
    public class SignOutNotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            //TODO:可根据登录用户分组缓存
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
