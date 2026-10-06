using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Locking;
using MediView.Studies.Application.Studies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Studies.Api.Studies;

internal static class StudyLockEndpoints
{
    public sealed record ForceReleaseRequest(string? Reason);

    public static IEndpointRouteBuilder MapStudyLockEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var studyLock = endpoints.MapGroup("/studies/{id:guid}/lock");

        studyLock.MapGet("/", GetOwner).RequireAuthorization();
        studyLock.MapPost("/heartbeat", Heartbeat).RequireAuthorization(AuthPolicies.DoctorOnly);
        studyLock.MapPost("/release", Release).RequireAuthorization(AuthPolicies.DoctorOnly);
        studyLock.MapPost("/force-release", ForceRelease).RequireAuthorization(AuthPolicies.AdminOnly);

        return endpoints;
    }

    private static async Task<Results<Ok<LockOwner>, NoContent>> GetOwner(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var owner = await sender.Send(new GetStudyLockOwnerQuery(id), cancellationToken);

        return owner is null ? TypedResults.NoContent() : TypedResults.Ok(owner);
    }

    private static async Task<NoContent> Heartbeat(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RenewStudyLockCommand(id, user.DoctorId()), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Release(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ReleaseStudyLockCommand(id, user.DoctorId()), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ForceRelease(
        Guid id,
        ForceReleaseRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ForceReleaseStudyLockCommand(id, user.UserId(), request.Reason ?? string.Empty);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Study not found", detail: result.Error.Message);
    }
}
