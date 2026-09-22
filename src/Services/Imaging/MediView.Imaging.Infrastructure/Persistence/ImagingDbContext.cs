using MediView.Imaging.Domain.Instances;
using Microsoft.EntityFrameworkCore;

namespace MediView.Imaging.Infrastructure.Persistence;

public sealed class ImagingDbContext(DbContextOptions<ImagingDbContext> options) : DbContext(options)
{
    public const string Schema = "imaging";

    public DbSet<Instance> Instances => Set<Instance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("imaging");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImagingDbContext).Assembly);
    }
}
