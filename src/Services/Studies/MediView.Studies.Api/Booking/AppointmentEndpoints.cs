using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Booking;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Studies.Api.Booking;

internal static class AppointmentEndpoints
{
    public sealed record BookAppointmentRequest(Guid DoctorId, DateTimeOffset ScheduledStart);

    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/appointments", Book).RequireAuthorization(AuthPolicies.PatientOnly);
        endpoints.MapGet("/studies/booked-slots", BookedSlots).RequireAuthorization();
        return endpoints;
    }

    private static async Task<Results<Created<BookedStudy>, ProblemHttpResult>> Book(
        BookAppointmentRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new BookCheckupCommand(user.UserId(), request.DoctorId, request.ScheduledStart);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return TypedResults.Created($"/studies/{result.Value.Id}", result.Value);
        }

        return result.Error == BookingErrors.OutsideSchedule
            ? TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Slot not offered", detail: result.Error.Message)
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: result.Error.Message);
    }

    private static async Task<Ok<IReadOnlyList<DateTimeOffset>>> BookedSlots(
        Guid doctorId,
        DateTimeOffset from,
        DateTimeOffset to,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListBookedSlotsQuery(doctorId, from, to), cancellationToken));
}
