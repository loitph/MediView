namespace MediView.Web.Api;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
