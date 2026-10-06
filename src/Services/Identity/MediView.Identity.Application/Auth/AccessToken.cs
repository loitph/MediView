namespace MediView.Identity.Application.Auth;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
