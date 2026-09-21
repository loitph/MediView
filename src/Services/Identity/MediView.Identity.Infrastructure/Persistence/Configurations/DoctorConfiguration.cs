using MediView.Identity.Domain.Doctors;
using MediView.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediView.Identity.Infrastructure.Persistence.Configurations;

internal sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasKey(doctor => doctor.Id);
        builder.Property(doctor => doctor.LicenseNumber).HasMaxLength(64);
        builder.Property(doctor => doctor.Specialty).HasMaxLength(100);
        builder.HasIndex(doctor => doctor.LicenseNumber).IsUnique();
        builder.HasIndex(doctor => doctor.UserId).IsUnique();
        builder.HasOne<User>().WithOne().HasForeignKey<Doctor>(doctor => doctor.UserId);
        builder.HasMany(doctor => doctor.Schedules)
            .WithOne()
            .HasForeignKey(schedule => schedule.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(doctor => doctor.Schedules).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasXminConcurrencyToken();
    }
}
