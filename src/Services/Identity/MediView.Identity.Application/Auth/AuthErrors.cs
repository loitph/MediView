using MediView.BuildingBlocks.Application;

namespace MediView.Identity.Application.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("auth.invalid_credentials", "Email or password is incorrect.");
}
