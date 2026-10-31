using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using MediView.BuildingBlocks.Application;
using MediView.Identity.Application.Users;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediView.Identity.Api.Users;

internal static class UserEndpoints
{
    public sealed record RenameUserRequest([Required, MaxLength(200)] string FullName);

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/me/name", Rename).RequireAuthorization();
        return endpoints;
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Rename(
        RenameUserRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RenameUserCommand(user.UserId(), request.FullName), cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Rename failed",
                detail: result.Error.Message);
    }
}
