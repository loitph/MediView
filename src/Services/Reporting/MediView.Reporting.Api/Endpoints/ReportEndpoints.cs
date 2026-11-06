using System.Security.Claims;
using MediView.Reporting.Api.Data;
using MediView.Reporting.Api.Entities;
using MediView.Shared;
using Microsoft.EntityFrameworkCore;

namespace MediView.Reporting.Api.Endpoints;

public static class ReportEndpoints
{
    public record StartReportRequest(Guid StudyId);

    public record UpdateReportRequest(string? Findings, string? Impression);

    public record AddMedicationRequest(string DrugName, string Dosage, string Frequency, string Duration);

    public record FinalizeReportRequest(ReportDecision Decision);

    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/reports", ListReports).RequireAuthorization();
        app.MapPost("/reports", StartReport).RequireAuthorization(Roles.Doctor);
        app.MapPut("/reports/{id:guid}", UpdateReport).RequireAuthorization(Roles.Doctor);
        app.MapPost("/reports/{id:guid}/medications", AddMedication).RequireAuthorization(Roles.Doctor);
        app.MapDelete("/reports/{id:guid}/medications/{medicationId:guid}", RemoveMedication).RequireAuthorization(Roles.Doctor);
        app.MapPost("/reports/{id:guid}/finalize", FinalizeReport).RequireAuthorization(Roles.Doctor);
    }

    private static async Task<IResult> ListReports(Guid studyId, ClaimsPrincipal user, ReportingDbContext db, StudiesClient studies)
    {
        var study = await studies.GetStudyAsync(studyId);
        if (study is null)
        {
            return Results.NotFound();
        }

        Guid? doctorId = user.IsInRole(Roles.Doctor) ? user.GetDoctorId() : null;

        var reports = await db.Reports
            .Where(r => r.StudyId == studyId)
            .Where(r => r.Status == ReportStatus.Finalized || r.DoctorId == doctorId)
            .OrderByDescending(r => r.Id)
            .ToListAsync();

        return Results.Ok(reports);
    }

    private static async Task<IResult> StartReport(StartReportRequest request, ClaimsPrincipal user, ReportingDbContext db, StudiesClient studies)
    {
        var doctorId = user.GetDoctorId();
        await EnsureDoctorIsReadingAsync(studies, request.StudyId, doctorId);

        var draft = await db.Reports.FirstOrDefaultAsync(r =>
            r.StudyId == request.StudyId && r.DoctorId == doctorId && r.Status == ReportStatus.Draft);

        if (draft is null)
        {
            draft = new Report
            {
                Id = Guid.CreateVersion7(),
                StudyId = request.StudyId,
                DoctorId = doctorId,
                DoctorName = user.GetName(),
                Status = ReportStatus.Draft,
            };
            db.Reports.Add(draft);
            await db.SaveChangesAsync();
        }

        return Results.Ok(new { draft.Id });
    }

    private static async Task<IResult> UpdateReport(
        Guid id,
        UpdateReportRequest request,
        ClaimsPrincipal user,
        ReportingDbContext db,
        StudiesClient studies)
    {
        var report = await FindReportToEditAsync(id, user, db, studies);
        if (report is null)
        {
            return Results.NotFound();
        }

        report.UpdateText(request.Findings, request.Impression);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> AddMedication(
        Guid id,
        AddMedicationRequest request,
        ClaimsPrincipal user,
        ReportingDbContext db,
        StudiesClient studies)
    {
        var report = await FindReportToEditAsync(id, user, db, studies);
        if (report is null)
        {
            return Results.NotFound();
        }

        var medication = report.AddMedication(request.DrugName, request.Dosage, request.Frequency, request.Duration);
        await db.SaveChangesAsync();
        return Results.Ok(new { medication.Id });
    }

    private static async Task<IResult> RemoveMedication(
        Guid id,
        Guid medicationId,
        ClaimsPrincipal user,
        ReportingDbContext db,
        StudiesClient studies)
    {
        var report = await FindReportToEditAsync(id, user, db, studies);
        if (report is null)
        {
            return Results.NotFound();
        }

        report.RemoveMedication(medicationId);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> FinalizeReport(
        Guid id,
        FinalizeReportRequest request,
        ClaimsPrincipal user,
        ReportingDbContext db,
        StudiesClient studies)
    {
        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == id);
        if (report is null)
        {
            return Results.NotFound();
        }

        if (report.DoctorId != user.GetDoctorId())
        {
            throw new DomainException("This report belongs to another doctor.");
        }

        if (report.Status == ReportStatus.Draft)
        {
            await EnsureDoctorIsReadingAsync(studies, report.StudyId, report.DoctorId);
            report.Finalize(request.Decision);
            await db.SaveChangesAsync();
        }
        else if (report.Decision != request.Decision)
        {
            throw new DomainException($"This report was finalized as {report.Decision} and cannot change.");
        }

        if (!await studies.FinishReadAsync(report.StudyId, request.Decision))
        {
            return Results.Problem(
                "The report is finalized, but the study could not be moved. Finalize again to retry.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.NoContent();
    }

    private static async Task<Report?> FindReportToEditAsync(Guid id, ClaimsPrincipal user, ReportingDbContext db, StudiesClient studies)
    {
        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == id);
        if (report is null)
        {
            return null;
        }

        if (report.DoctorId != user.GetDoctorId())
        {
            throw new DomainException("This report belongs to another doctor.");
        }

        await EnsureDoctorIsReadingAsync(studies, report.StudyId, report.DoctorId);
        return report;
    }

    private static async Task EnsureDoctorIsReadingAsync(StudiesClient studies, Guid studyId, Guid doctorId)
    {
        var study = await studies.GetStudyAsync(studyId);
        if (study is null || study.Status != "InProgress" || study.DoctorId != doctorId)
        {
            throw new DomainException("You are not reading this study. Open the study to write its report.");
        }
    }
}
