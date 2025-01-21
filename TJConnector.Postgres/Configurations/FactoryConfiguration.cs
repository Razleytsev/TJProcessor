using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJConnector.Postgres.Entities;

namespace TJConnector.Postgres.Configurations;

internal sealed class FactoryConfiguration : IEntityTypeConfiguration<Factory>
{
    public void Configure(EntityTypeBuilder<Factory> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasData(
            new Factory { Id = 1, Name = "DefaulFactory", ExternalUid = new Guid("326e5c13-4280-4078-be01-ebed9d73716a") }
            );
    }
}
