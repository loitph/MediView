using System.Globalization;
using System.Security.Claims;

namespace MediView.BuildingBlocks.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(MediViewClaims.Subject) ?? throw MissingClaim(MediViewClaims.Subject), CultureInfo.InvariantCulture);

    public static Guid DoctorId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(MediViewClaims.DoctorId) ?? throw MissingClaim(MediViewClaims.DoctorId), CultureInfo.InvariantCulture);

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(MediViewClaims.Name) ?? throw MissingClaim(MediViewClaims.Name);

    private static InvalidOperationException MissingClaim(string claim) =>
        new($"The access token carries no '{claim}' claim.");
}
