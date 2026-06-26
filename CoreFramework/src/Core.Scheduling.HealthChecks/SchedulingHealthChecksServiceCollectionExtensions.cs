using System;
using System.Collections.Generic;
using Core.Scheduling.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Core.Scheduling.HealthChecks 依赖注入扩展。</summary>
    public static class SchedulingHealthChecksServiceCollectionExtensions
    {
        /// <summary>默认健康检查名。</summary>
        public const string DefaultName = "scheduling";

        /// <summary>
        /// 注册调度健康检查。底层走 <see cref="HealthChecksBuilderAddCheckExtensions.AddCheck{T}"/>,
        /// 把 <see cref="SchedulingHealthCheck"/> 串到 IHealthChecksBuilder 上;业务侧仍可链式追加 <c>AddCheck&lt;Other&gt;(...)</c>。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configureOptions">可选阈值配置回调。</param>
        /// <param name="name">健康检查名,默认 <c>scheduling</c>。</param>
        /// <param name="failureStatus">阈值未满足时的兜底状态;null 让健康检查自己定夺。</param>
        /// <param name="tags">健康检查 tag,便于 <c>/health/ready</c> 之类的分组路由。</param>
        public static IHealthChecksBuilder AddSchedulingHealthCheck(
            this IServiceCollection services,
            Action<SchedulingHealthCheckOptions> configureOptions = null,
            string name = DefaultName,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (configureOptions != null)
                services.Configure(configureOptions);
            else
                services.AddOptions<SchedulingHealthCheckOptions>();

            return services.AddHealthChecks()
                .AddCheck<SchedulingHealthCheck>(name, failureStatus, tags ?? Array.Empty<string>());
        }

        /// <summary>同上,但走 <see cref="IHealthChecksBuilder"/> 链式 API,适合已有 <c>AddHealthChecks()</c> 链的工程。</summary>
        public static IHealthChecksBuilder AddSchedulingHealthCheck(
            this IHealthChecksBuilder builder,
            Action<SchedulingHealthCheckOptions> configureOptions = null,
            string name = DefaultName,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (configureOptions != null)
                builder.Services.Configure(configureOptions);
            else
                builder.Services.AddOptions<SchedulingHealthCheckOptions>();

            return builder.AddCheck<SchedulingHealthCheck>(name, failureStatus, tags ?? Array.Empty<string>());
        }
    }
}
