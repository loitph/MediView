using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Identity.Application.Doctors;
using MediView.Identity.Application.Patients;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Identity.Api.Internal;

internal static class InternalEndpoints
{
    public static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var snapshots = endpoints.MapGroup("/internal").RequireAuthorization();

        snapshots.MapGet("/people/{patientUserId:guid}", Patient);
        snapshots.MapGet("/doctors/{id:guid}", Doctor);

        return endpoints;
    }

    private static async Task<Results<Ok<PatientSnapshot>, ForbidHttpResult, ProblemHttpResult>> Patient(
        Guid patientUserId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (user.UserId() != patientUserId && !user.IsInRole(MediViewRoles.Admin))
        {
            return TypedResults.Forbid();
        }

        var result = await sender.Send(new GetPatientSnapshotQuery(patientUserId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Patient not found", detail: result.Error.Message);
    }

    private static async Task<Results<Ok<DoctorSummary>, ProblemHttpResult>> Doctor(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDoctorQuery(id), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Doctor not found", detail: result.Error.Message);
    }
}
