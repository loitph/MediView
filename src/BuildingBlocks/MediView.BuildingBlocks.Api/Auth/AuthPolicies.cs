namespace MediView.BuildingBlocks.Api.Auth;

public static class AuthPolicies
{
    public const string PatientOnly = nameof(PatientOnly);
    public const string DoctorOnly = nameof(DoctorOnly);
    public const string AdminOnly = nameof(AdminOnly);
}
