using MediView.Reporting.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediView.Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(report => report.Id);
        builder.Property(report => report.DoctorName).HasMaxLength(200);
        builder.Property(report => report.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(report => report.Decision).HasConversion<string>().HasMaxLength(16);
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasIndex(report => report.StudyId);
        builder.HasIndex(report => new { report.DoctorId, report.Status });

        builder.OwnsMany(report => report.Medications, ConfigureMedications);
        builder.Navigation(report => report.Medications).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureMedications(OwnedNavigationBuilder<Report, ReportMedication> medications)
    {
        medications.ToTable("report_medications");
        medications.WithOwner().HasForeignKey("ReportId");
        medications.HasKey(medication => medication.Id);
        medications.Property(medication => medication.Id).ValueGeneratedNever();
        medications.Property(medication => medication.DrugName).HasMaxLength(200);
        medications.Property(medication => medication.Dosage).HasMaxLength(100);
        medications.Property(medication => medication.Frequency).HasMaxLength(100);
        medications.Property(medication => medication.Duration).HasMaxLength(100);
        medications.HasIndex("ReportId");
    }
}
