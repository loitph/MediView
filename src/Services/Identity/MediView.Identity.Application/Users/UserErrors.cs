using MediView.BuildingBlocks.Application;

namespace MediView.Identity.Application.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => new("user.not_found", $"User {userId} does not exist.");
}
