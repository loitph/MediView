using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Reporting.Application.Reports;
using MediView.Reporting.Domain.Reports;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Reporting.Api.Reports;

internal static class ReportEndpoints
{
    public sealed record StartReportRequest(Guid StudyId);

    public sealed record UpdateReportRequest(string? Findings, string? Impression);

    public sealed record AddMedicationRequest(string? DrugName, string? Dosage, string? Frequency, string? Duration);

    public sealed record FinalizeReportRequest(ReportDecision Decision);

    public sealed record CreatedId(Guid Id);

    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/reports", List).RequireAuthorization();

        var writes = endpoints.MapGroup("/reports").RequireAuthorization(AuthPolicies.DoctorOnly);
        writes.MapPost("/", Start);
        writes.MapPut("/{id:guid}", Update);
        writes.MapPost("/{id:guid}/medications", AddMedication);
        writes.MapDelete("/{id:guid}/medications/{medicationId:guid}", RemoveMedication);
        writes.MapPost("/{id:guid}/finalize", FinalizeReport);

        return endpoints;
    }

    private static async Task<Results<Ok<IReadOnlyList<ReportView>>, ProblemHttpResult>> List(
        Guid studyId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var doctorId = user.IsInRole(MediViewRoles.Doctor) ? user.DoctorId() : (Guid?)null;
        var result = await sender.Send(new ListReportsQuery(studyId, doctorId), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : NotFound(result.Error);
    }

    private static async Task<Created<CreatedId>> Start(
        StartReportRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new StartReportCommand(request.StudyId, user.DoctorId(), user.DisplayName()), cancellationToken);
        return TypedResults.Created($"/reports/{id}", new CreatedId(id));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Update(
        Guid id,
        UpdateReportRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken) =>
        NoContentOr(await sender.Send(
            new UpdateReportTextCommand(id, user.DoctorId(), request.Findings, request.Impression),
            cancellationToken));

    private static async Task<Results<Created<CreatedId>, ProblemHttpResult>> AddMedication(
        Guid id,
        AddMedicationRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AddMedicationCommand(
            id,
            user.DoctorId(),
            request.DrugName ?? string.Empty,
            request.Dosage ?? string.Empty,
            request.Frequency ?? string.Empty,
            request.Duration ?? string.Empty);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/reports/{id}/medications/{result.Value}", new CreatedId(result.Value))
            : NotFound(result.Error);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemoveMedication(
        Guid id,
        Guid medicationId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken) =>
        NoContentOr(await sender.Send(new RemoveMedicationCommand(id, user.DoctorId(), medicationId), cancellationToken));

    private static async Task<Results<NoContent, ProblemHttpResult>> FinalizeReport(
        Guid id,
        FinalizeReportRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new FinalizeReportCommand(id, user.DoctorId(), request.Decision), cancellationToken);

        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        return result.Error == ReportErrors.StudyNotUpdated
            ? TypedResults.Problem(statusCode: StatusCodes.Status502BadGateway, title: "Study not updated", detail: result.Error.Message)
            : NotFound(result.Error);
    }

    private static Results<NoContent, ProblemHttpResult> NoContentOr(Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : NotFound(result.Error);

    private static ProblemHttpResult NotFound(Error error) =>
        TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: error.Message);
}
