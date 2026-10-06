using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Locking;
using MediView.Studies.Application.Studies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Studies.Api.Studies;

internal static class StudyEndpoints
{
    public static IEndpointRouteBuilder MapStudyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/studies", List).RequireAuthorization();
        endpoints.MapGet("/studies/{id:guid}", Detail).RequireAuthorization();
        endpoints.MapPost("/studies/{id:guid}/open", Open).RequireAuthorization(AuthPolicies.DoctorOnly);
        return endpoints;
    }

    private static async Task<Results<Ok<IReadOnlyList<StudySummary>>, ForbidHttpResult>> List(
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (user.AsStudyViewer() is not { } viewer)
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await sender.Send(new ListStudiesQuery(viewer), cancellationToken));
    }

    private static async Task<Results<Ok<StudyDetail>, ForbidHttpResult, ProblemHttpResult>> Detail(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (user.AsStudyViewer() is not { } viewer)
        {
            return TypedResults.Forbid();
        }

        var result = await sender.Send(new GetStudyDetailQuery(id, viewer), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Study not found", detail: result.Error.Message);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Open(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var doctor = new LockOwner(user.DoctorId(), user.DisplayName());
        var result = await sender.Send(new OpenStudyCommand(id, doctor), cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Study not found",
                detail: result.Error.Message);
    }
}
