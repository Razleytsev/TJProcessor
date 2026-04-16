using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using TJConnector.Postgres.Entities;

namespace TJConnector.Postgres.Configurations;

internal sealed class TestRunConfiguration : IEntityTypeConfiguration<TestRun>
{
    public void Configure(EntityTypeBuilder<TestRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RecordDate).HasDefaultValueSql("NOW()");
        builder.Property(r => r.User).HasMaxLength(50);
        builder.Property(r => r.StatusMessage).HasMaxLength(1000);
        builder.Property(r => r.MastercaseSscc).HasMaxLength(200);

        var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        builder.Property(r => r.PhaseHistory)
            .HasConversion(
                v => JsonConvert.SerializeObject(v, jsonSettings),
                v => JsonConvert.DeserializeObject<TestRunPhaseLog[]>(v, jsonSettings) ?? Array.Empty<TestRunPhaseLog>())
            .HasColumnType("jsonb");

        builder.Property(r => r.PackCodes)
            .HasConversion(
                v => JsonConvert.SerializeObject(v, jsonSettings),
                v => JsonConvert.DeserializeObject<string[]>(v, jsonSettings))
            .HasColumnType("jsonb");

        builder.Property(r => r.BundleCodes)
            .HasConversion(
                v => JsonConvert.SerializeObject(v, jsonSettings),
                v => JsonConvert.DeserializeObject<string[]>(v, jsonSettings))
            .HasColumnType("jsonb");
    }
}
