using MediView.BuildingBlocks.Application;

namespace MediView.Studies.Application.Locking;

public sealed record ReleaseStudyLockCommand(Guid StudyId, Guid DoctorId) : ICommand<Result>;

internal sealed class ReleaseStudyLockCommandHandler(IStudyLock studyLock) : ICommandHandler<ReleaseStudyLockCommand, Result>
{
    public async Task<Result> Handle(ReleaseStudyLockCommand request, CancellationToken cancellationToken)
    {
        if (!await studyLock.Release(request.StudyId, request.DoctorId))
        {
            throw StudyLockConflict.NotHeldBy(request.StudyId, request.DoctorId);
        }

        return Result.Success();
    }
}
