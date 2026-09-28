using MediView.BuildingBlocks.Application;

namespace MediView.Studies.Application.Locking;

public sealed record RenewStudyLockCommand(Guid StudyId, Guid DoctorId) : ICommand<Result>;

internal sealed class RenewStudyLockCommandHandler(IStudyLock studyLock) : ICommandHandler<RenewStudyLockCommand, Result>
{
    public async Task<Result> Handle(RenewStudyLockCommand request, CancellationToken cancellationToken)
    {
        if (!await studyLock.Renew(request.StudyId, request.DoctorId))
        {
            throw StudyLockConflict.NotHeldBy(request.StudyId, request.DoctorId);
        }

        return Result.Success();
    }
}
