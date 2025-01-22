using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJConnector.Postgres.Entities;

namespace TJConnector.Postgres.Configurations;

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(o => o.Id);

        builder.HasData(
            new Location { Id = 1, Name = "DefaulLocation", ExternalUid = new Guid("d4ee03fa-497e-4228-8db4-505c13e6b3bb") }
            );
    }
}
