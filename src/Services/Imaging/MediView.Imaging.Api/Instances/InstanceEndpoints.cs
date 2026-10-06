using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Imaging.Application.Instances;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Imaging.Api.Instances;

internal static class InstanceEndpoints
{
    public const long MaxUploadBytes = 256L * 1024 * 1024;

    private const string DicomMediaType = "application/dicom";

    public static IEndpointRouteBuilder MapInstanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/studies/{studyId:guid}/images", Import)
            .RequireAuthorization(AuthPolicies.AdminOnly)
            .DisableAntiforgery()
            .WithFormOptions(multipartBodyLengthLimit: MaxUploadBytes);

        endpoints.MapGet("/studies/{studyId:guid}/instances", List)
            .RequireAuthorization(policy => policy.RequireRole(MediViewRoles.Doctor, MediViewRoles.Admin));

        endpoints.MapGet("/instances/{id:guid}/file", File)
            .RequireAuthorization(policy => policy.RequireRole(MediViewRoles.Doctor, MediViewRoles.Admin));

        return endpoints;
    }

    private static async Task<Results<Ok<ImportedImages>, ProblemHttpResult>> Import(
        Guid studyId,
        IFormFileCollection files,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var uploads = files.Select(file => new UploadedFile(file.FileName, file.OpenReadStream)).ToList();
        var result = await sender.Send(new ImportImagesCommand(studyId, uploads), cancellationToken);

        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        return result.Error switch
        {
            var error when error == ImagingErrors.StudyNotFound(studyId) =>
                Problem(StatusCodes.Status404NotFound, "Study not found", error),
            var error when error == ImagingErrors.StudyNotUpdated =>
                Problem(StatusCodes.Status502BadGateway, "Study not updated", error),
            var error => Problem(StatusCodes.Status400BadRequest, "Images not imported", error),
        };
    }

    private static async Task<Results<Ok<IReadOnlyList<InstanceSummary>>, ProblemHttpResult>> List(
        Guid studyId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListInstancesQuery(studyId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : Problem(StatusCodes.Status404NotFound, "Study not found", result.Error);
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> File(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken) =>
        await sender.Send(new OpenInstanceFileQuery(id), cancellationToken) is { } content
            ? TypedResults.Stream(content, DicomMediaType)
            : TypedResults.NotFound();

    private static ProblemHttpResult Problem(int statusCode, string title, Error error) =>
        TypedResults.Problem(statusCode: statusCode, title: title, detail: error.Message);
}
