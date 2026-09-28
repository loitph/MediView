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
        endpoints.MapPost("/studies/{id:guid}/open", Open).RequireAuthorization(AuthPolicies.DoctorOnly);
        return endpoints;
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
