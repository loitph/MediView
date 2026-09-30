using System.ComponentModel.DataAnnotations;
using MediView.BuildingBlocks.Application;
using MediView.Identity.Application.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Identity.Api.Auth;

internal static class AuthEndpoints
{
    public const int MinimumPasswordLength = 8;

    public sealed record LoginRequest(string Email, string Password);

    public sealed record RegisterRequest(
        [Required, EmailAddress, MaxLength(320)] string Email,
        [Required, MinLength(MinimumPasswordLength), MaxLength(128)] string Password,
        [Required, MaxLength(200)] string FullName);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", Login).AllowAnonymous();
        endpoints.MapPost("/auth/register", Register).AllowAnonymous();
        return endpoints;
    }

    private static async Task<Results<Ok<AccessToken>, ProblemHttpResult>> Login(
        LoginRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Login failed",
                detail: result.Error.Message);
    }

    private static async Task<Results<Created<RegisteredPatient>, ProblemHttpResult>> Register(
        RegisterRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterPatientCommand(request.Email, request.Password, request.FullName),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/internal/people/{result.Value.UserId}", result.Value)
            : TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Registration failed",
                detail: result.Error.Message);
    }
}
