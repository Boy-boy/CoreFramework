using Core.Alert;
using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Alert.Sqlite
{
    /// <summary>
    /// SQLite 存储适配模块。
    /// 通过配置 Alert:Storage，将默认内存存储替换为 SQLite 实现。
    /// </summary>
    [DependsOn(typeof(CoreAlertModule))]
    public class CoreAlertSqliteModule : CoreModuleBase
    {
        public CoreAlertSqliteModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<AlertOptions>(options =>
            {
                options.AddSqlite(Configuration.GetSection("Alert:Storage"));
            });
        }
    }
}
