namespace MediView.Web.Api;

public sealed record RegisterRequest(string Email, string Password, string FullName);
