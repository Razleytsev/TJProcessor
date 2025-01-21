using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJConnector.Postgres.Entities;

namespace TJConnector.Postgres;
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<CodeOrder> CodeOrders { get; set; }
    public DbSet<CodeOrderContent> CodeOrdersContents { get; set; }
    public DbSet<Factory> Factories { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<MarkingLine> MarkingLines { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<PackageRequest> PackageRequests { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
