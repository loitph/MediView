using MediView.BuildingBlocks.Application;

namespace MediView.Identity.Application.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("auth.invalid_credentials", "Email or password is incorrect.");
    public static readonly Error EmailTaken = new("auth.email_taken", "An account with this email already exists.");
}
