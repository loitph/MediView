using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Locking;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Studies.Api.Studies;

internal static class StudyLockEndpoints
{
    public static IEndpointRouteBuilder MapStudyLockEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var studyLock = endpoints.MapGroup("/studies/{id:guid}/lock");

        studyLock.MapGet("/", GetOwner).RequireAuthorization();
        studyLock.MapPost("/heartbeat", Heartbeat).RequireAuthorization(AuthPolicies.DoctorOnly);
        studyLock.MapPost("/release", Release).RequireAuthorization(AuthPolicies.DoctorOnly);

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
}
