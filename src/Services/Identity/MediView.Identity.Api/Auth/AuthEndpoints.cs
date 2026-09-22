using MediView.BuildingBlocks.Application;
using MediView.Identity.Application.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Identity.Api.Auth;

internal static class AuthEndpoints
{
    public sealed record LoginRequest(string Email, string Password);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", Login).AllowAnonymous();
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
}
