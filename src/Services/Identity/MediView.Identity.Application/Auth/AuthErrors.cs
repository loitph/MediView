using MediView.BuildingBlocks.Application;

namespace MediView.Identity.Application.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("Email or password is incorrect.");
    public static readonly Error EmailTaken = new("An account with this email already exists.");
}
