using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJConnector.Postgres.Entities;

namespace TJConnector.Postgres.Configurations;

internal sealed class MarkingLineConfiguration : IEntityTypeConfiguration<MarkingLine>
{
    public void Configure(EntityTypeBuilder<MarkingLine> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasData(
            new MarkingLine { Id = 1, Name = "DefaulMarkingLine", ExternalUid = new Guid("0e3cf053-078c-44a7-a198-74cb4d66caf4") }
            );
    }
}
