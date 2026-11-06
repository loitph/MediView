using MediView.Identity.Api.Data;
using MediView.Identity.Api.Entities;
using MediView.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Api.Endpoints;

public static class DoctorEndpoints
{
    public record CreateDoctorRequest(
        string FullName,
        string Email,
        string Password,
        string LicenseNumber,
        string Specialty,
        List<ShiftRequest> Shifts);

    public record ShiftRequest(DayOfWeek DayOfWeek, TimeOnly Start, TimeOnly End, int SlotMinutes);

    public record DoctorSummary(Guid Id, string FullName, string Specialty);

    public record AvailableSlot(DateTimeOffset Start, DateTimeOffset End);

    public static void MapDoctorEndpoints(this WebApplication app)
    {
        app.MapPost("/admin/doctors", CreateDoctor).RequireAuthorization(Roles.Admin);
        app.MapGet("/doctors", ListDoctors);
        app.MapGet("/doctors/{id:guid}/availability", GetAvailability);
        app.MapGet("/internal/doctors/{id:guid}", GetDoctor).RequireAuthorization();
    }

    private static async Task<IResult> CreateDoctor(
        CreateDoctorRequest request,
        IdentityDbContext db,
        PasswordHasher<User> hasher)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return Results.Problem("An account with this email already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        var licenseNumber = request.LicenseNumber.Trim();
        if (await db.Doctors.AnyAsync(d => d.LicenseNumber == licenseNumber))
        {
            return Results.Problem("A doctor with this licence number already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        var user = new User { Id = Guid.CreateVersion7(), Email = email, FullName = request.FullName.Trim(), Role = Role.Doctor };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        var doctor = new Doctor
        {
            Id = Guid.CreateVersion7(),
            User = user,
            LicenseNumber = licenseNumber,
            Specialty = request.Specialty.Trim(),
        };

        foreach (var shift in request.Shifts)
        {
            doctor.Schedules.Add(new DoctorSchedule
            {
                Id = Guid.CreateVersion7(),
                DayOfWeek = shift.DayOfWeek,
                StartTime = shift.Start,
                EndTime = shift.End,
                SlotMinutes = shift.SlotMinutes,
            });
        }

        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        return Results.Ok(new DoctorSummary(doctor.Id, user.FullName, doctor.Specialty));
    }

    private static async Task<IResult> ListDoctors(IdentityDbContext db)
    {
        var doctors = await db.Doctors
            .OrderBy(d => d.User.FullName)
            .Select(d => new DoctorSummary(d.Id, d.User.FullName, d.Specialty))
            .ToListAsync();

        return Results.Ok(doctors);
    }

    private static async Task<IResult> GetDoctor(Guid id, IdentityDbContext db)
    {
        var doctor = await db.Doctors
            .Where(d => d.Id == id)
            .Select(d => new DoctorSummary(d.Id, d.User.FullName, d.Specialty))
            .FirstOrDefaultAsync();

        return doctor is null ? Results.NotFound() : Results.Ok(doctor);
    }

    private static async Task<IResult> GetAvailability(Guid id, DateOnly from, DateOnly to, IdentityDbContext db)
    {
        if (to < from || to > from.AddDays(30))
        {
            return Results.Problem("Pick a range of at most 31 days.", statusCode: StatusCodes.Status400BadRequest);
        }

        var doctor = await db.Doctors.Include(d => d.Schedules).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return Results.NotFound();
        }

        var slots = new List<AvailableSlot>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            foreach (var schedule in doctor.Schedules.Where(s => s.DayOfWeek == day.DayOfWeek))
            {
                var slotStart = day.ToDateTime(schedule.StartTime);
                var shiftEnd = day.ToDateTime(schedule.EndTime);

                while (slotStart.AddMinutes(schedule.SlotMinutes) <= shiftEnd)
                {
                    var slotEnd = slotStart.AddMinutes(schedule.SlotMinutes);
                    var slot = new AvailableSlot(InClinicTimeZone(slotStart), InClinicTimeZone(slotEnd));
                    if (slot.Start > DateTimeOffset.UtcNow)
                    {
                        slots.Add(slot);
                    }

                    slotStart = slotEnd;
                }
            }
        }

        return Results.Ok(slots);
    }

    private static DateTimeOffset InClinicTimeZone(DateTime localTime) =>
        new(localTime, TimeZoneInfo.Local.GetUtcOffset(localTime));
}
