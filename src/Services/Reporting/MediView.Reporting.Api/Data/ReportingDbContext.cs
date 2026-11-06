using MediView.Reporting.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediView.Reporting.Api.Data;

public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reporting");

        modelBuilder.Entity<Report>(report =>
        {
            report.Property(r => r.DoctorName).HasMaxLength(200);
            report.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
            report.Property(r => r.Decision).HasConversion<string>().HasMaxLength(16);
            report.Property<uint>("Version").IsRowVersion();
            report.HasIndex(r => r.StudyId);
            report.HasIndex(r => new { r.DoctorId, r.Status });

            report.OwnsMany(r => r.Medications, medication =>
            {
                medication.ToTable("report_medications");
                medication.WithOwner().HasForeignKey("ReportId");
                medication.HasKey(m => m.Id);
                medication.Property(m => m.Id).ValueGeneratedNever();
                medication.Property(m => m.DrugName).HasMaxLength(200);
                medication.Property(m => m.Dosage).HasMaxLength(100);
                medication.Property(m => m.Frequency).HasMaxLength(100);
                medication.Property(m => m.Duration).HasMaxLength(100);
                medication.HasIndex("ReportId");
            });
        });
    }
}
