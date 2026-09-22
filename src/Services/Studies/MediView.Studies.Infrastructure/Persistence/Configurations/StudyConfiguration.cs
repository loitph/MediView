using MediView.Studies.Domain.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediView.Studies.Infrastructure.Persistence.Configurations;

internal sealed class StudyConfiguration : IEntityTypeConfiguration<Study>
{
    public const string StudyNumberSequence = "study_number_seq";

    public void Configure(EntityTypeBuilder<Study> builder)
    {
        builder.HasKey(study => study.Id);
        builder.Property(study => study.StudyNumber)
            .HasMaxLength(16)
            .HasDefaultValueSql($"'STU-' || nextval('{StudiesDbContext.Schema}.{StudyNumberSequence}')")
            .ValueGeneratedOnAdd();
        builder.Property(study => study.PatientName).HasMaxLength(200);
        builder.Property(study => study.PatientMrn).HasMaxLength(16);
        builder.Property(study => study.DoctorName).HasMaxLength(200);
        builder.Property(study => study.Priority).HasConversion<string>().HasMaxLength(16);
        builder.Property(study => study.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasIndex(study => study.StudyNumber).IsUnique();
        builder.HasIndex(study => new { study.DoctorId, study.ScheduledStart }).IsUnique();
        builder.HasIndex(study => study.PatientId);

        builder.OwnsMany(study => study.History, ConfigureHistory);
        builder.Navigation(study => study.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureHistory(OwnedNavigationBuilder<Study, StudyStatusHistory> history)
    {
        history.ToTable("study_status_history");
        history.WithOwner().HasForeignKey("StudyId");
        history.Property<long>("Id").UseIdentityAlwaysColumn();
        history.HasKey("Id");
        history.Property(entry => entry.FromStatus).HasConversion<string>().HasMaxLength(16);
        history.Property(entry => entry.ToStatus).HasConversion<string>().HasMaxLength(16);
        history.Property(entry => entry.Reason).HasMaxLength(500);
        history.HasIndex("StudyId", nameof(StudyStatusHistory.ChangedAt));
    }
}
