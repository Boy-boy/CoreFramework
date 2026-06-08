using Microsoft.Extensions.DependencyInjection;

namespace Core.Alert
{
    /// <summary>
    /// 告警模块选项。
    /// 主要用于挂接不同存储后端的扩展注册逻辑。
    /// </summary>
    public class AlertOptions
    {
        public AlertOptions()
        {
            Extensions = new List<IAlertOptionsExtensions>();
        }

        /// <summary>
        /// 已挂接的告警扩展集合。
        /// </summary>
        public List<IAlertOptionsExtensions> Extensions { get; set; }
    }

    public static class AlertOptionsConfigureExtensions
    {
        /// <summary>
        /// 向选项中添加一个扩展。
        /// </summary>
        public static void AddExtensions(this AlertOptions options, IAlertOptionsExtensions alertOptionsExtensions)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (alertOptionsExtensions == null)
            {
                throw new ArgumentNullException(nameof(alertOptionsExtensions));
            }

            options.Extensions.Add(alertOptionsExtensions);
        }

        /// <summary>
        /// 执行所有告警扩展，把附加服务注册进 DI 容器。
        /// </summary>
        public static void Configure(this AlertOptions options, IServiceCollection services)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }
        }
    }
}
