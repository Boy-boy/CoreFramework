using Core.EventBus.Storage.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.EventBus.Storage.EfCore.Configurations
{
    /// <summary>
    /// <see cref="DeadLetterMessageEntity"/> 的 EF 配置;
    /// 与 <see cref="OutboxMessageConfiguration"/> 几乎相同,仅多 <c>DeadAtUtc</c> 索引以加速按时间段排查。
    /// </summary>
    public class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessageEntity>
    {
        public const string TableName = "EventBus_DeadLetter";

        public void Configure(EntityTypeBuilder<DeadLetterMessageEntity> builder)
        {
            builder.ToTable(TableName);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Version).IsRequired();
            builder.Property(x => x.AssemblyName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.MessageName).IsRequired().HasMaxLength(512);
            builder.Property(x => x.MessageData).IsRequired();
            builder.Property(x => x.CreateTime).IsRequired();
            builder.Property(x => x.UtcTime).IsRequired();
            builder.Property(x => x.RetryCount).IsRequired();
            builder.Property(x => x.LastError).HasMaxLength(4000);
            builder.Property(x => x.DeadAtUtc).IsRequired();

            // 运维查询常按"最近 N 小时新增死信"过滤;DeadAtUtc 单列索引足够
            builder.HasIndex(x => x.DeadAtUtc)
                .HasDatabaseName("IX_EventBus_DeadLetter_DeadAt");
        }
    }
}
