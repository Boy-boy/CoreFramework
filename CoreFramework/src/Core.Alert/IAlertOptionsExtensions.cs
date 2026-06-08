using Microsoft.Extensions.DependencyInjection;

namespace Core.Alert
{
    /// <summary>
    /// 告警模块扩展点。
    /// 存储适配包通过该接口把自己的依赖注册进容器，并覆盖默认存储实现。
    /// </summary>
    public interface IAlertOptionsExtensions
    {
        void AddServices(IServiceCollection services);
    }
}
