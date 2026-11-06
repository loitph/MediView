using System.Security.Claims;

namespace MediView.Shared;

public static class UserClaims
{
    public const string UserId = "sub";
    public const string Name = "name";
    public const string Role = "role";
    public const string DoctorId = "doctor_id";

    public static Guid GetUserId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(UserId)!);

    public static Guid GetDoctorId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(DoctorId)!);

    public static string GetName(this ClaimsPrincipal user) => user.FindFirstValue(Name)!;
}
