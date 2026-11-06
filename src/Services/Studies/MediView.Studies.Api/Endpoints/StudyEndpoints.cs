using System.Security.Claims;
using MediView.Shared;
using MediView.Studies.Api.Data;
using MediView.Studies.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediView.Studies.Api.Endpoints;

public static class StudyEndpoints
{
    public enum StudyActor
    {
        Patient,
        Doctor,
        Admin,
    }

    public enum ReadDecision
    {
        Complete,
        ReDiagnosis,
    }

    public record StudySummary(
        Guid Id,
        string StudyNumber,
        string PatientName,
        string PatientMrn,
        Guid DoctorId,
        string DoctorName,
        DateTimeOffset ScheduledStart,
        StudyStatus Status,
        bool ImagesImported);

    public record StatusChange(StudyStatus? From, StudyStatus To, DateTimeOffset ChangedAt, StudyActor ChangedBy, string? Reason);

    public record StudyDetail(StudySummary Study, List<StatusChange> History);

    public record OverrideRequest(StudyStatus To, string Reason);

    public record FinishReadRequest(ReadDecision Decision);

    public static void MapStudyEndpoints(this WebApplication app)
    {
        app.MapGet("/studies", ListStudies).RequireAuthorization();
        app.MapGet("/studies/{id:guid}", GetStudy).RequireAuthorization();
        app.MapPost("/studies/{id:guid}/open", OpenStudy).RequireAuthorization(Roles.Doctor);
        app.MapPost("/studies/{id:guid}/override", OverrideStatus).RequireAuthorization(Roles.Admin);
        app.MapPost("/internal/studies/{id:guid}/images-imported", MarkImagesImported).RequireAuthorization(Roles.Admin);
        app.MapPost("/internal/studies/{id:guid}/finish-read", FinishRead).RequireAuthorization(Roles.Doctor);
    }

    private static async Task<IResult> ListStudies(ClaimsPrincipal user, StudiesDbContext db)
    {
        var query = db.Studies.AsQueryable();

        if (user.IsInRole(Roles.Patient))
        {
            var patientId = user.GetUserId();
            query = query.Where(s => s.PatientId == patientId);
        }
        else if (user.IsInRole(Roles.Doctor))
        {
            var doctorId = user.GetDoctorId();
            query = query.Where(s => s.DoctorId == doctorId);
        }

        var studies = await query.OrderBy(s => s.ScheduledStart).ToListAsync();
        return Results.Ok(studies.Select(ToSummary));
    }

    private static async Task<IResult> GetStudy(Guid id, ClaimsPrincipal user, StudiesDbContext db)
    {
        var study = await db.Studies.FirstOrDefaultAsync(s => s.Id == id);
        if (study is null)
        {
            return Results.NotFound();
        }

        if (user.IsInRole(Roles.Patient) && study.PatientId != user.GetUserId())
        {
            return Results.NotFound();
        }

        var history = study.History
            .OrderBy(h => h.ChangedAt)
            .Select(h => new StatusChange(h.FromStatus, h.ToStatus, h.ChangedAt, WhoChanged(h, study), h.Reason))
            .ToList();

        return Results.Ok(new StudyDetail(ToSummary(study), history));
    }

    private static async Task<IResult> OpenStudy(Guid id, ClaimsPrincipal user, StudiesDbContext db)
    {
        var study = await db.Studies.FirstOrDefaultAsync(s => s.Id == id);
        if (study is null)
        {
            return Results.Problem("This study does not exist.", statusCode: StatusCodes.Status404NotFound);
        }

        var doctorId = user.GetDoctorId();
        var alreadyReading = study.Status == StudyStatus.InProgress && study.DoctorId == doctorId;
        if (!alreadyReading)
        {
            study.StartRead(doctorId);
            await db.SaveChangesAsync();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> OverrideStatus(Guid id, OverrideRequest request, ClaimsPrincipal user, StudiesDbContext db)
    {
        var study = await db.Studies.FirstOrDefaultAsync(s => s.Id == id);
        if (study is null)
        {
            return Results.NotFound();
        }

        study.Override(request.To, user.GetUserId(), request.Reason);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> MarkImagesImported(Guid id, StudiesDbContext db)
    {
        var study = await db.Studies.FirstOrDefaultAsync(s => s.Id == id);
        if (study is null)
        {
            return Results.NotFound();
        }

        if (study.ImagesImportedAt is null)
        {
            study.ImagesImportedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> FinishRead(Guid id, FinishReadRequest request, ClaimsPrincipal user, StudiesDbContext db)
    {
        var study = await db.Studies.FirstOrDefaultAsync(s => s.Id == id);
        if (study is null)
        {
            return Results.NotFound();
        }

        if (study.DoctorId != user.GetDoctorId())
        {
            return Results.Forbid();
        }

        if (request.Decision == ReadDecision.Complete && study.Status != StudyStatus.Done)
        {
            study.Complete();
        }
        else if (request.Decision == ReadDecision.ReDiagnosis && study.Status != StudyStatus.ReDiagnosis)
        {
            study.RequestReDiagnosis();
        }

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static StudySummary ToSummary(Study study) => new(
        study.Id,
        study.StudyNumber,
        study.PatientName,
        study.PatientMrn,
        study.DoctorId,
        study.DoctorName,
        study.ScheduledStart,
        study.Status,
        study.ImagesImportedAt is not null);

    private static StudyActor WhoChanged(StudyStatusHistory change, Study study)
    {
        if (change.ChangedBy == study.DoctorId)
        {
            return StudyActor.Doctor;
        }

        if (change.ChangedBy == study.PatientId)
        {
            return StudyActor.Patient;
        }

        return StudyActor.Admin;
    }
}
