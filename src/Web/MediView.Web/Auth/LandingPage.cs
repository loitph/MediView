using System.Security.Claims;
using MediView.Shared;

namespace MediView.Web.Auth;

public static class LandingPage
{
    public static string For(ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin))
        {
            return "admin/doctors";
        }

        if (user.IsInRole(Roles.Doctor))
        {
            return "worklist";
        }

        if (user.IsInRole(Roles.Patient))
        {
            return "my-studies";
        }

        return "";
    }
}
