using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;

namespace MediView.Web.Auth;

public static class LandingPage
{
    public const string Home = "";
    public const string AdminDoctors = "admin/doctors";

    public static string For(ClaimsPrincipal user) =>
        user.IsInRole(MediViewRoles.Admin) ? AdminDoctors : Home;
}
