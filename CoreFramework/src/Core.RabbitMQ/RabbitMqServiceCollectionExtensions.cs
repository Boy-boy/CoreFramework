using Core.RabbitMQ;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMqServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, Action<RabbitMqOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.TryAddSingleton<IRabbitMqPersistentConnection, DefaultRabbitMqPersistentConnection>();
            services.TryAddSingleton<IRabbitMqMessageConsumerManager, DefaultRabbitMqMessageConsumerManager>();
            services.Configure(configureOptions);
            return services;
        }

        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.TryAddSingleton<IRabbitMqPersistentConnection, DefaultRabbitMqPersistentConnection>();
            services.TryAddSingleton<IRabbitMqMessageConsumerManager, DefaultRabbitMqMessageConsumerManager>();
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
    }
}
