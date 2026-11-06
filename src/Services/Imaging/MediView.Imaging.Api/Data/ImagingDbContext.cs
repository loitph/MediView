using MediView.Imaging.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediView.Imaging.Api.Data;

public class ImagingDbContext(DbContextOptions<ImagingDbContext> options) : DbContext(options)
{
    public DbSet<Instance> Instances => Set<Instance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("imaging");

        modelBuilder.Entity<Instance>(instance =>
        {
            instance.Property(i => i.SeriesInstanceUid).HasMaxLength(64);
            instance.Property(i => i.SopInstanceUid).HasMaxLength(64);
            instance.Property(i => i.StoragePath).HasMaxLength(500);
            instance.HasIndex(i => new { i.StudyId, i.SopInstanceUid }).IsUnique();
            instance.HasIndex(i => new { i.StudyId, i.SeriesInstanceUid, i.InstanceNumber });
        });
    }
}
