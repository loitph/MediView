using System.Security.Claims;
using MediView.Identity.Api.Data;
using MediView.Shared;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Api.Endpoints;

public static class PatientEndpoints
{
    public record PatientInfo(Guid UserId, string FullName, string Mrn);

    public static void MapPatientEndpoints(this WebApplication app)
    {
        app.MapGet("/internal/people/{userId:guid}", GetPatient).RequireAuthorization();
    }

    private static async Task<IResult> GetPatient(Guid userId, ClaimsPrincipal user, IdentityDbContext db)
    {
        var isSelf = user.GetUserId() == userId;
        if (!isSelf && !user.IsInRole(Roles.Admin))
        {
            return Results.Forbid();
        }

        var patient = await db.Patients
            .Where(p => p.UserId == userId)
            .Select(p => new PatientInfo(p.UserId, p.User.FullName, p.Mrn))
            .FirstOrDefaultAsync();

        return patient is null ? Results.NotFound() : Results.Ok(patient);
    }
}
