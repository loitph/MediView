using MediView.BuildingBlocks.Application;
using MediView.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MediView.Identity.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<AccessToken>>;

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IDoctorRepository doctors,
    IPasswordHasher<User> passwordHasher,
    IAccessTokenIssuer tokenIssuer) : ICommandHandler<LoginCommand, Result<AccessToken>>
{
    public async Task<Result<AccessToken>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || !PasswordMatches(user, request.Password))
        {
            return Result.Failure<AccessToken>(AuthErrors.InvalidCredentials);
        }

        var doctorId = user.Role == Role.Doctor
            ? await doctors.FindIdByUserIdAsync(user.Id, cancellationToken)
            : null;

        return tokenIssuer.Issue(user, doctorId);
    }

    private bool PasswordMatches(User user, string password) =>
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
