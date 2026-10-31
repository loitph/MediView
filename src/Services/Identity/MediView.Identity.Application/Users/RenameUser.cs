using MediView.BuildingBlocks.Application;
using MediView.Identity.Application.Auth;

namespace MediView.Identity.Application.Users;

public sealed record RenameUserCommand(Guid UserId, string FullName) : ICommand<Result>;

internal sealed class RenameUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : ICommandHandler<RenameUserCommand, Result>
{
    public async Task<Result> Handle(RenameUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        user.Rename(request.FullName);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
