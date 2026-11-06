using System.Security.Claims;
using MediView.Shared;
using MediView.Studies.Api.Data;
using MediView.Studies.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediView.Studies.Api.Endpoints;

public static class AppointmentEndpoints
{
    public record BookAppointmentRequest(Guid DoctorId, DateTimeOffset ScheduledStart);

    public static void MapAppointmentEndpoints(this WebApplication app)
    {
        app.MapPost("/appointments", BookAppointment).RequireAuthorization(Roles.Patient);
        app.MapGet("/studies/booked-slots", ListBookedSlots).RequireAuthorization();
    }

    private static async Task<IResult> BookAppointment(
        BookAppointmentRequest request,
        ClaimsPrincipal user,
        StudiesDbContext db,
        IdentityClient identity)
    {
        var start = request.ScheduledStart.ToUniversalTime();

        var patient = await identity.GetPatientAsync(user.GetUserId());
        var doctor = await identity.GetDoctorAsync(request.DoctorId);
        if (patient is null || doctor is null)
        {
            return Results.Problem("Patient or doctor not found.", statusCode: StatusCodes.Status404NotFound);
        }

        if (!await identity.DoctorOffersSlotAsync(doctor.Id, start))
        {
            return Results.Problem(
                "The doctor does not offer that slot. Pick a time from their availability.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (await db.Studies.AnyAsync(s => s.DoctorId == doctor.Id && s.ScheduledStart == start))
        {
            return Results.Problem("That slot is already booked. Pick another time.", statusCode: StatusCodes.Status409Conflict);
        }

        var study = new Study
        {
            Id = Guid.CreateVersion7(),
            PatientId = patient.UserId,
            PatientName = patient.FullName,
            PatientMrn = patient.Mrn,
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            ScheduledStart = start,
            Status = StudyStatus.ToDo,
        };

        study.History.Add(new StudyStatusHistory
        {
            ToStatus = StudyStatus.ToDo,
            ChangedBy = patient.UserId,
            ChangedAt = DateTimeOffset.UtcNow,
        });

        db.Studies.Add(study);
        await db.SaveChangesAsync();

        return Results.Ok(new { study.Id, study.StudyNumber });
    }

    private static async Task<IResult> ListBookedSlots(Guid doctorId, DateTimeOffset from, DateTimeOffset to, StudiesDbContext db)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        var bookedStarts = await db.Studies
            .Where(s => s.DoctorId == doctorId && s.ScheduledStart >= fromUtc && s.ScheduledStart < toUtc)
            .Select(s => s.ScheduledStart)
            .ToListAsync();

        return Results.Ok(bookedStarts);
    }
}
