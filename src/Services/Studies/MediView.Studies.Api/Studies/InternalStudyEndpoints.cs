using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Studies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Studies.Api.Studies;

internal static class InternalStudyEndpoints
{
    public sealed record FinishReadRequest(ReadDecision Decision);

    public static IEndpointRouteBuilder MapInternalStudyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/internal/studies/{id:guid}/images-imported", ImagesImported)
            .RequireAuthorization(AuthPolicies.AdminOnly);
        endpoints.MapPost("/internal/studies/{id:guid}/finish-read", FinishRead)
            .RequireAuthorization(AuthPolicies.DoctorOnly);
        return endpoints;
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ImagesImported(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkImagesImportedCommand(id), cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Study not found", detail: result.Error.Message);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> FinishRead(
        Guid id,
        FinishReadRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new FinishReadCommand(id, user.DoctorId(), request.Decision), cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Study not found", detail: result.Error.Message);
    }
}
