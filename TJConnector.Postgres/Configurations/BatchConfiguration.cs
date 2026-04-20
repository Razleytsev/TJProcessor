using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres.Entities;
using Newtonsoft.Json;

namespace TJConnector.Postgres.Configurations;

internal sealed class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.StatusHistoryJson)
            .HasConversion(
                      v => JsonConvert.SerializeObject(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                      v => JsonConvert.DeserializeObject<StatusHistory[]>(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) ?? new StatusHistory[0]
            )
            .HasColumnType("jsonb");
        builder.Property(o => o.RecordDate).HasDefaultValueSql("NOW()");
        builder.Property(o => o.User).HasMaxLength(20);
        builder.Property(o => o.Description).HasMaxLength(100);
        //builder.HasMany(b => b.CodeOrders)
        //    .WithOne(b => b.Batch)
        //    .HasForeignKey(b => b.BatchId)
        //    .IsRequired(false);
    }
}
