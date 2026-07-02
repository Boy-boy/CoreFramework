using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Core.Kafka
{
    /// <summary>
    /// Kafka 基础设施层的根配置;由
    /// <see cref="Microsoft.Extensions.DependencyInjection.KafkaServiceCollectionExtensions"/> 绑到 IOptions 系统。
    /// </summary>
    public class KafkaOptions
    {
        public KafkaOptions()
        {
            Connection = new KafkaConnectionConfigure();
        }

        /// <summary>Kafka 连接配置（brokers、librdkafka client 配置）。</summary>
        public KafkaConnectionConfigure Connection { get; set; }

        /// <summary>
        /// 消费失败时 <see cref="Confluent.Kafka.IConsumer{TKey,TValue}.Seek"/> 回失败 offset 重投前的退避,
        /// 避免 poison message 在 PollLoop 上热循环。默认 5 秒。
        /// </summary>
        /// <remarks>
        /// 与 inbox 去重协同:成功 handler 在 inbox 命中后跳过,失败 handler 借由重投不断重试。
        /// poison message 的最终截断口是 <see cref="MaxConsecutiveFailures"/>;再精细的"丢消息可观测"
        /// (如转 dead-letter topic) 由业务层在 inbox 上跟踪计数后自行实现。
        /// </remarks>
        public TimeSpan FailureBackoff { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 同条 offset 连续失败的最大重试次数。达到上限后框架 commit + LogWarning 跳过该条消息,
        /// 避免 poison message 永久阻塞整个 partition。默认 5 次;设为 0 关闭(无限重试,旧版语义)。
        /// </summary>
        /// <remarks>
        /// 命中上限后框架只 log + commit,不会自动发到 dead-letter topic —— 想要"可观测的丢失"
        /// 请用监控/告警订阅 LogWarning,或在 handler 内基于 inbox 失败计数显式投递 DLT 后吞掉异常
        /// (让本条算作成功 commit)。
        /// </remarks>
        public int MaxConsecutiveFailures { get; set; } = 5;

        /// <summary>
        /// 把 <paramref name="source"/> 的所有可读写公有属性浅拷贝到本实例。
        /// </summary>
        /// <remarks>
        /// 主要给上层桥接场景用(如 EventBus.Kafka 把 <c>EventBusKafkaOptions.Broker</c> 透传到 <c>IOptions&lt;KafkaOptions&gt;</c>):
        /// 反射拷贝可写属性,新增字段无需修改桥接代码 —— 之前手写字段映射的方式,加字段就漏。
        /// 引用类型走浅拷贝(如 <see cref="Connection"/> 共享同一实例),这与直接持字段一致。
        /// </remarks>
        public void CopyFrom(KafkaOptions source)
        {
            if (source == null) return;
            if (ReferenceEquals(source, this)) return;

            foreach (var prop in typeof(KafkaOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                // 索引器等带参数属性跳过,避免 GetValue/SetValue 抛 TargetParameterCountException
                if (prop.GetIndexParameters().Length > 0) continue;
                prop.SetValue(this, prop.GetValue(source));
            }
        }
    }

    /// <summary>
    /// <see cref="KafkaOptions"/> 启动期校验;把明显不合法的取值(负数、零退避)在配置绑定后立即报出,
    /// 避免运行时才踩到"重试无退避 = 热循环刷爆日志"这类隐蔽故障。
    /// </summary>
    internal sealed class KafkaOptionsValidator : IValidateOptions<KafkaOptions>
    {
        public ValidateOptionsResult Validate(string name, KafkaOptions options)
        {
            if (options == null)
                return ValidateOptionsResult.Fail("KafkaOptions is null.");

            var errors = new List<string>();

            if (options.MaxConsecutiveFailures < 0)
                errors.Add($"KafkaOptions.MaxConsecutiveFailures 不能为负数(当前:{options.MaxConsecutiveFailures})。设为 0 表示无限重试。");

            if (options.FailureBackoff <= TimeSpan.Zero)
                errors.Add($"KafkaOptions.FailureBackoff 必须为正(当前:{options.FailureBackoff}),否则 poison message 会在 PollLoop 上热循环。");

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }
    }
}
