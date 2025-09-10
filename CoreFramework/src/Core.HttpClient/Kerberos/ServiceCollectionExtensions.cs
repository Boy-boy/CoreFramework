using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.HttpClient.Kerberos
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加 Kerberos 认证相关服务到依赖注入容器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddKerberosAuthentication(this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // 添加内存缓存
            services.AddMemoryCache();

            // 添加配置选项
            services.Configure<KerberosOptions>(configuration.GetSection(KerberosOptions.SectionName));

            // 注册 Kerberos 认证服务
            services.AddSingleton<IKerberosAuthService, KerberosAuthService>();

            // 注册 Kerberos 认证处理器
            services.AddTransient<KerberosAuthHandler>();

            // 注册带 Kerberos 认证的 HttpClient
            services.AddHttpClient("KerberizedClient")
                .AddHttpMessageHandler<KerberosAuthHandler>();

            // 注册配置变更监控（如果需要热重载）
            services.AddSingleton<IOptionsChangeTokenSource<KerberosOptions>>(
                new ConfigurationChangeTokenSource<KerberosOptions>(KerberosOptions.SectionName, configuration));

            return services;
        }

        /// <summary>
        /// 添加 Kerberos 认证相关服务到依赖注入容器（使用指定的配置节名称）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddKerberosAuthentication(this IServiceCollection services,
            IConfiguration configuration, string sectionName)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

            // 添加内存缓存
            services.AddMemoryCache();

            // 添加配置选项
            services.Configure<KerberosOptions>(configuration.GetSection(sectionName));

            // 注册 Kerberos 认证服务
            services.AddSingleton<IKerberosAuthService, KerberosAuthService>();

            // 注册 Kerberos 认证处理器
            services.AddTransient<KerberosAuthHandler>();

            // 注册带 Kerberos 认证的 HttpClient
            services.AddHttpClient("KerberizedClient")
                .AddHttpMessageHandler<KerberosAuthHandler>();

            // 注册配置变更监控（如果需要热重载）
            services.AddSingleton<IOptionsChangeTokenSource<KerberosOptions>>(
                new ConfigurationChangeTokenSource<KerberosOptions>(sectionName, configuration));

            return services;
        }

        /// <summary>
        /// 添加 Kerberos 认证相关服务到依赖注入容器（使用自定义配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项委托</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddKerberosAuthentication(this IServiceCollection services,
            Action<KerberosOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // 添加内存缓存
            services.AddMemoryCache();

            // 添加配置选项
            services.Configure(configureOptions);

            // 注册 Kerberos 认证服务
            services.AddSingleton<IKerberosAuthService, KerberosAuthService>();

            // 注册 Kerberos 认证处理器
            services.AddTransient<KerberosAuthHandler>();

            // 注册带 Kerberos 认证的 HttpClient
            services.AddHttpClient("KerberizedClient")
                .AddHttpMessageHandler<KerberosAuthHandler>();

            return services;
        }

        /// <summary>
        /// 添加命名的带 Kerberos 认证的 HttpClient
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="name">HttpClient 名称</param>
        /// <param name="configureClient">配置 HttpClient 委托</param>
        /// <returns>服务集合</returns>
        public static IHttpClientBuilder AddKerberosHttpClient(this IServiceCollection services, string name,
            Action<IServiceProvider, System.Net.Http.HttpClient> configureClient = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("HttpClient name cannot be null or empty", nameof(name));

            var builder = services.AddHttpClient(name)
                .AddHttpMessageHandler<KerberosAuthHandler>();

            if (configureClient != null)
            {
                builder.ConfigureHttpClient(configureClient);
            }

            return builder;
        }

        /// <summary>
        /// 添加命名的带 Kerberos 认证的 HttpClient
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="name">HttpClient 名称</param>
        /// <param name="baseAddress">基础地址</param>
        /// <returns>服务集合</returns>
        public static IHttpClientBuilder AddKerberosHttpClient(this IServiceCollection services, string name,
            string baseAddress)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("HttpClient name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(baseAddress))
                throw new ArgumentException("Base address cannot be null or empty", nameof(baseAddress));

            return services.AddHttpClient(name)
                .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseAddress))
                .AddHttpMessageHandler<KerberosAuthHandler>();
        }
    }
}
