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

internal sealed class CodeOrderContentConfiguration : IEntityTypeConfiguration<CodeOrderContent>
{
    public void Configure(EntityTypeBuilder<CodeOrderContent> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasOne<CodeOrder>().WithOne().HasForeignKey<CodeOrderContent>(c => c.Id);
        builder.Property(c => c.DownloadHistory)
            .HasConversion(
                      v => JsonConvert.SerializeObject(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                      v => JsonConvert.DeserializeObject<DownloadHistory>(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
            )
            .HasColumnType("jsonb");
        builder.Property(o => o.RecordDate).HasDefaultValueSql("NOW()");
    }
}
