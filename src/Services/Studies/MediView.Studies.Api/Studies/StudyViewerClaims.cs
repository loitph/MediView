using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.Studies.Application.Studies;

namespace MediView.Studies.Api.Studies;

internal static class StudyViewerClaims
{
    public static StudyViewer? AsStudyViewer(this ClaimsPrincipal user) => user switch
    {
        _ when user.IsInRole(MediViewRoles.Admin) => new AdminViewer(),
        _ when user.IsInRole(MediViewRoles.Doctor) => new DoctorViewer(user.DoctorId()),
        _ when user.IsInRole(MediViewRoles.Patient) => new PatientViewer(user.UserId()),
        _ => null,
    };
}
