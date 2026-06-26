using Core.Scheduling;
using Microsoft.Extensions.Configuration;

public static class ScheduleDescriptorConfigurationExtensions
{
    public const string ConfigurationRoot = "Scheduling:Descriptors";

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

        // 1. 利用框架自带的 Binder 自动映射和解析类型（自动处理 TimeSpan, Enum, bool 等）
        var dto = section.Get<ScheduleDto>();
        if (dto?.Kind == null)
        {
            throw new InvalidOperationException($"Configuration section '{path}' is invalid or missing required key 'Kind'.");
        }

        var startDelay = dto.StartDelay ?? TimeSpan.Zero;
        var allowConcurrent = dto.AllowConcurrentExecution ?? false;

        // 2. 根据类型调用对应的工厂方法
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

    /// <summary>
    /// 仅供内部配置绑定的可变 DTO
    /// </summary>
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