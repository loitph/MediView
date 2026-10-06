using MediView.Identity.Domain.Doctors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediView.Identity.Infrastructure.Persistence.Configurations;

internal sealed class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Id).ValueGeneratedNever();
        builder.Property(schedule => schedule.DayOfWeek).HasConversion<string>().HasMaxLength(9);
        builder.HasIndex(schedule => new { schedule.DoctorId, schedule.DayOfWeek });
        builder.ToTable("doctor_schedules", table => table.HasCheckConstraint(
            "ck_doctor_schedules_time_range",
            "start_time < end_time AND slot_minutes > 0"));
    }
}
