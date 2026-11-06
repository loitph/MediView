using MediView.Studies.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediView.Studies.Api.Data;

public class StudiesDbContext(DbContextOptions<StudiesDbContext> options) : DbContext(options)
{
    public DbSet<Study> Studies => Set<Study>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("studies");
        modelBuilder.HasSequence<long>("study_number_seq").StartsAt(1_000);

        modelBuilder.Entity<Study>(study =>
        {
            study.Property(s => s.StudyNumber)
                .HasMaxLength(16)
                .HasDefaultValueSql("'STU-' || nextval('studies.study_number_seq')")
                .ValueGeneratedOnAdd();
            study.Property(s => s.PatientName).HasMaxLength(200);
            study.Property(s => s.PatientMrn).HasMaxLength(16);
            study.Property(s => s.DoctorName).HasMaxLength(200);
            study.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
            study.Property<uint>("Version").IsRowVersion();
            study.HasIndex(s => s.StudyNumber).IsUnique();
            study.HasIndex(s => new { s.DoctorId, s.ScheduledStart }).IsUnique();
            study.HasIndex(s => s.PatientId);

            study.OwnsMany(s => s.History, history =>
            {
                history.ToTable("study_status_history");
                history.WithOwner().HasForeignKey("StudyId");
                history.Property<long>("Id").UseIdentityAlwaysColumn();
                history.HasKey("Id");
                history.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(16);
                history.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(16);
                history.Property(h => h.Reason).HasMaxLength(500);
                history.HasIndex("StudyId", "ChangedAt");
            });
        });
    }
}
