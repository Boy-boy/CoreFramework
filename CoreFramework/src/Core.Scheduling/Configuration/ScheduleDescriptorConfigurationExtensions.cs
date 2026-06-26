using System;
using Microsoft.Extensions.Configuration;

namespace Core.Scheduling.Configuration
{
    /// <summary>
    /// 从 <see cref="IConfiguration"/> 读取 <see cref="ScheduleDescriptor"/>,
    /// handler 注入 <see cref="IConfiguration"/> 后即可让运维通过 <c>appsettings.json</c> 调节奏,免重编。
    /// </summary>
    /// <remarks>
    /// 节路径固定:<c>Scheduling:Descriptors:{HandlerCode}</c>。
    /// 节点缺失 / <c>Kind</c> 缺失 / 必填字段缺失均在启动期抛 <see cref="InvalidOperationException"/>。
    /// </remarks>
    public static class ScheduleDescriptorConfigurationExtensions
    {
        /// <summary>本扩展使用的根配置节路径。</summary>
        public const string ConfigurationRoot = "Scheduling:Descriptors";

        /// <summary>从 <c>Scheduling:Descriptors:{handlerCode}</c> 读取并构造一个 <see cref="ScheduleDescriptor"/>。</summary>
        /// <exception cref="InvalidOperationException">节点缺失或必填字段缺失。</exception>
        public static ScheduleDescriptor FromConfiguration(this IConfiguration configuration, string handlerCode)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(handlerCode))
                throw new ArgumentException("handlerCode must be non-empty.", nameof(handlerCode));

            var path = $"{ConfigurationRoot}:{handlerCode}";
            var section = configuration.GetSection(path);

            if (!section.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{path}' is missing.");
            }

            var dto = section.Get<ScheduleDto>();
            if (dto?.Kind == null)
            {
                throw new InvalidOperationException($"Configuration section '{path}' is invalid or missing required key 'Kind'.");
            }

            var startDelay = dto.StartDelay ?? TimeSpan.Zero;
            var allowConcurrent = dto.AllowConcurrentExecution ?? false;

            return dto.Kind switch
            {
                ScheduleKind.FixedInterval => ScheduleDescriptor.FixedInterval(
                    dto.Interval ?? throw new InvalidOperationException($"Missing required key 'Interval' for FixedInterval in '{path}'."),
                    startDelay,
                    allowConcurrent,
                    dto.MaxBackoff),

                ScheduleKind.Cron => ScheduleDescriptor.Cron(
                    string.IsNullOrWhiteSpace(dto.CronExpression)
                        ? throw new InvalidOperationException($"Missing required key 'CronExpression' for Cron in '{path}'.")
                        : dto.CronExpression,
                    dto.TimeZoneId,
                    startDelay,
                    allowConcurrent,
                    dto.MaxBackoff),

                _ => throw new InvalidOperationException($"Unsupported ScheduleKind '{dto.Kind}' in '{path}'.")
            };
        }

        /// <summary>仅供配置绑定的可变 DTO。</summary>
        private sealed class ScheduleDto
        {
            public ScheduleKind? Kind { get; init; }
            public TimeSpan? Interval { get; init; }
            public string CronExpression { get; init; }
            public string TimeZoneId { get; init; }
            public TimeSpan? StartDelay { get; init; }
            public bool? AllowConcurrentExecution { get; init; }
            public TimeSpan? MaxBackoff { get; init; }
        }
    }
}
