using MediView.Imaging.Domain.Instances;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediView.Imaging.Infrastructure.Persistence.Configurations;

internal sealed class InstanceConfiguration : IEntityTypeConfiguration<Instance>
{
    private const int DicomUidMaxLength = 64;

    public void Configure(EntityTypeBuilder<Instance> builder)
    {
        builder.HasKey(instance => instance.Id);
        builder.Property(instance => instance.SeriesInstanceUid).HasMaxLength(DicomUidMaxLength);
        builder.Property(instance => instance.SopInstanceUid).HasMaxLength(DicomUidMaxLength);
        builder.Property(instance => instance.StoragePath).HasMaxLength(500);

        builder.HasIndex(instance => instance.SopInstanceUid).IsUnique();
        builder.HasIndex(instance => new { instance.StudyId, instance.SeriesInstanceUid, instance.InstanceNumber });
    }
}
