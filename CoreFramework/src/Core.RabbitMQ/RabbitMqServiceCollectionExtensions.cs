using Core.RabbitMQ;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMqServiceCollectionExtensions
    {
        /// <summary>只注册 RabbitMQ infrastructure singleton,不绑定任何 <see cref="RabbitMqOptions"/> 来源。</summary>
        /// <remarks>给上层模块用:它自己用 <c>AddOptions&lt;RabbitMqOptions&gt;().Configure&lt;IOptions&lt;XxxOptions&gt;&gt;(...)</c> 把配置联动过来,避免在 appsettings 里写两遍。</remarks>
        public static IServiceCollection AddRabbitMq(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IRabbitMqPersistentConnection, DefaultRabbitMqPersistentConnection>();
            services.TryAddSingleton<IRabbitMqMessageConsumerManager, DefaultRabbitMqMessageConsumerManager>();
            // Metrics 一律 singleton,Meter 内部按名字聚合,不需要多实例。TryAdd 让上层可以自替换。
            services.TryAddSingleton<RabbitMqMetrics>();
            // 启动期数值合法性校验;非法配置立刻抛
            services.PostConfigure<RabbitMqOptions>(o => o.ValidateNumericLimits());
            return services;
        }

        public static IServiceCollection AddRabbitMq(this IServiceCollection services, Action<RabbitMqOptions> configureOptions)
        {
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.AddRabbitMq();
            services.Configure(configureOptions);
            return services;
        }

        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddRabbitMq();
            services.Configure<RabbitMqOptions>(configuration);
            return services;
        }

        /// <summary>
        /// 注册 <see cref="IRabbitMqPublishChannelPool"/>(Singleton,一池绑一个 exchange)。
        /// </summary>
        /// <remarks>
        /// <para>本方法把 pool 的构造细节(ctor 参数、依赖项解析)封装在 Core.RabbitMQ 内部,
        /// 让上层调用方只关心"用哪个 exchange、池大小多少"这两件事。</para>
        /// <para>pool 绑定 exchange / poolSize 在<b>首次解析时</b>由 accessor 委托完成,
        /// 因此 accessor 可以晚于本方法调用:常见用法是 <c>sp => sp.GetRequiredService&lt;IOptions&lt;XxxOptions&gt;&gt;().Value.ExchangeName</c>,
        /// 即便 XxxOptions 由别的 extension 后挂入也能正确取值。</para>
        /// <para><c>TryAdd</c> 语义:已存在自定义注册时不覆盖,符合 Core.RabbitMQ 其他注册的一贯风格。</para>
        /// </remarks>
        /// <param name="services">DI 容器。</param>
        /// <param name="exchangeNameAccessor">解析 exchange 名的委托;在 SP 首次解析 pool 时被调用一次。</param>
        /// <param name="poolSizeAccessor">解析池容量的委托;在 SP 首次解析 pool 时被调用一次。</param>
        public static IServiceCollection AddRabbitMqPublishChannelPool(
            this IServiceCollection services,
            Func<IServiceProvider, string> exchangeNameAccessor,
            Func<IServiceProvider, int> poolSizeAccessor)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (exchangeNameAccessor == null)
                throw new ArgumentNullException(nameof(exchangeNameAccessor));
            if (poolSizeAccessor == null)
                throw new ArgumentNullException(nameof(poolSizeAccessor));

            services.TryAddSingleton<IRabbitMqPublishChannelPool>(sp =>
                new RabbitMqPublishChannelPool(
                    sp.GetRequiredService<IRabbitMqPersistentConnection>(),
                    exchangeNameAccessor(sp),
                    poolSizeAccessor(sp),
                    sp.GetRequiredService<ILogger<RabbitMqPublishChannelPool>>()));
            return services;
        }

        /// <summary>
        /// 把 <see cref="RabbitMqHealthCheck"/> 挂进 <see cref="IHealthChecksBuilder"/>。
        /// 需要先 <c>services.AddHealthChecks()</c>。
        /// </summary>
        /// <param name="builder">HealthChecks builder。</param>
        /// <param name="name">健康检查名,默认 "rabbitmq"。</param>
        /// <param name="failureStatus">失败时上报的状态,默认 <see cref="HealthStatus.Unhealthy"/>。</param>
        /// <param name="tags">tag 列表,便于 UI 按标签分组过滤。</param>
        public static IHealthChecksBuilder AddRabbitMqHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "rabbitmq",
            HealthStatus? failureStatus = null,
            System.Collections.Generic.IEnumerable<string> tags = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new RabbitMqHealthCheck(sp.GetRequiredService<IRabbitMqPersistentConnection>()),
                failureStatus,
                tags));
        }
    }
}
