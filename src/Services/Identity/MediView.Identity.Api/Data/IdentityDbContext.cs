using MediView.Identity.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Api.Data;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.HasSequence<long>("patient_mrn_seq").StartsAt(100_000);

        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Email).HasMaxLength(320);
            user.Property(u => u.PasswordHash).HasMaxLength(512);
            user.Property(u => u.FullName).HasMaxLength(200);
            user.Property(u => u.Role).HasConversion<string>().HasMaxLength(16);
            user.Property<uint>("Version").IsRowVersion();
            user.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Patient>(patient =>
        {
            patient.Property(p => p.Mrn)
                .HasMaxLength(16)
                .HasDefaultValueSql("'MRN-' || nextval('identity.patient_mrn_seq')")
                .ValueGeneratedOnAdd();
            patient.Property<uint>("Version").IsRowVersion();
            patient.HasIndex(p => p.Mrn).IsUnique();
            patient.HasIndex(p => p.UserId).IsUnique();
            patient.HasOne(p => p.User).WithOne().HasForeignKey<Patient>(p => p.UserId);
        });

        modelBuilder.Entity<Doctor>(doctor =>
        {
            doctor.Property(d => d.LicenseNumber).HasMaxLength(64);
            doctor.Property(d => d.Specialty).HasMaxLength(100);
            doctor.Property<uint>("Version").IsRowVersion();
            doctor.HasIndex(d => d.LicenseNumber).IsUnique();
            doctor.HasIndex(d => d.UserId).IsUnique();
            doctor.HasOne(d => d.User).WithOne().HasForeignKey<Doctor>(d => d.UserId);
            doctor.HasMany(d => d.Schedules).WithOne().HasForeignKey(s => s.DoctorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DoctorSchedule>(schedule =>
        {
            schedule.ToTable("doctor_schedules", table => table.HasCheckConstraint(
                "ck_doctor_schedules_time_range",
                "start_time < end_time AND slot_minutes > 0"));
            schedule.Property(s => s.Id).ValueGeneratedNever();
            schedule.Property(s => s.DayOfWeek).HasConversion<string>().HasMaxLength(9);
            schedule.HasIndex(s => new { s.DoctorId, s.DayOfWeek });
        });
    }
}
