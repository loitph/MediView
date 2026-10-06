using System.ComponentModel.DataAnnotations;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Identity.Api.Auth;
using MediView.Identity.Application.Doctors;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Identity.Api.Doctors;

internal static class DoctorEndpoints
{
    public sealed record CreateDoctorRequest(
        [Required, MaxLength(200)] string FullName,
        [Required, EmailAddress, MaxLength(320)] string Email,
        [Required, MinLength(AuthEndpoints.MinimumPasswordLength), MaxLength(128)] string Password,
        [Required, MaxLength(64)] string LicenseNumber,
        [Required, MaxLength(100)] string Specialty,
        [Required, MinLength(1), MaxLength(7)] IReadOnlyList<ShiftRequest> Shifts);

    public sealed record ShiftRequest(
        DayOfWeek DayOfWeek,
        TimeOnly Start,
        TimeOnly End,
        [Range(5, 240)] int SlotMinutes);

    public static IEndpointRouteBuilder MapDoctorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/admin/doctors", Create).RequireAuthorization(AuthPolicies.AdminOnly);
        endpoints.MapGet("/doctors", List).AllowAnonymous();
        endpoints.MapGet("/doctors/{id:guid}/availability", Availability).AllowAnonymous();
        return endpoints;
    }

    private static async Task<Results<Created<DoctorSummary>, ProblemHttpResult>> Create(
        CreateDoctorRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var shifts = request.Shifts
            .Select(shift => new WeeklyShift(shift.DayOfWeek, shift.Start, shift.End, shift.SlotMinutes))
            .ToList();
        var command = new CreateDoctorCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.LicenseNumber,
            request.Specialty,
            shifts);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/internal/doctors/{result.Value.Id}", result.Value)
            : TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Doctor not created",
                detail: result.Error.Message);
    }

    private static async Task<Ok<IReadOnlyList<DoctorSummary>>> List(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListDoctorsQuery(), cancellationToken));

    private static async Task<Results<Ok<IReadOnlyList<AvailableSlot>>, ProblemHttpResult>> Availability(
        Guid id,
        DateOnly from,
        DateOnly to,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDoctorAvailabilityQuery(id, from, to), cancellationToken);

        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        return result.Error == DoctorErrors.InvalidRange
            ? TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid range", detail: result.Error.Message)
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Doctor not found", detail: result.Error.Message);
    }
}
