using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJConnector.Postgres.Entities;
using Newtonsoft.Json;

namespace TJConnector.Postgres.Configurations;

internal sealed class PackageRequestConfiguration : IEntityTypeConfiguration<PackageRequest>
{
    public void Configure(EntityTypeBuilder<PackageRequest> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.StatusHistory)
            .HasConversion(
                      v => JsonConvert.SerializeObject(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                      v => JsonConvert.DeserializeObject<StatusHistory>(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
            )
            .HasColumnType("jsonb");
        builder.Property(p => p.Filename).HasMaxLength(100);
        builder.Property(p => p.User).HasMaxLength(20);
        builder.Property(o => o.RecordDate).HasDefaultValueSql("NOW()");
    }
}
