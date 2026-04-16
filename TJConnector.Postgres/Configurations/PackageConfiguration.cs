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

internal sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Content)
            .HasConversion(
                      v => JsonConvert.SerializeObject(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                      v => JsonConvert.DeserializeObject<List<PackageContent>>(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
            )
            .HasColumnType("jsonb");
        builder.Property(p => p.StatusHistory)
            .HasConversion(
                      v => JsonConvert.SerializeObject(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                      v => JsonConvert.DeserializeObject<StatusHistory[]>(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) ?? new StatusHistory[0]
            )
            .HasColumnType("jsonb");

        builder.HasIndex(p => p.SSCCCode).IsUnique();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.SSCCCode).HasMaxLength(100);
        builder.Property(p => p.Code).HasMaxLength(100);
        builder.Property(o => o.RecordDate).HasDefaultValueSql("NOW()");
    }
}


