using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;

namespace MediView.Web.Auth;

public static class LandingPage
{
    public const string Home = "";
    public const string AdminDoctors = "admin/doctors";
    public const string Worklist = "worklist";
    public const string MyStudies = "my-studies";

    public static string For(ClaimsPrincipal user) => user switch
    {
        _ when user.IsInRole(MediViewRoles.Admin) => AdminDoctors,
        _ when user.IsInRole(MediViewRoles.Doctor) => Worklist,
        _ when user.IsInRole(MediViewRoles.Patient) => MyStudies,
        _ => Home,
    };
}
