using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Studies;

namespace MediView.Studies.Application.Locking;

public sealed record ForceReleaseStudyLockCommand(Guid StudyId, Guid AdminId, string Reason) : ICommand<Result>;

internal sealed class ForceReleaseStudyLockCommandHandler(
    IStudyRepository studies,
    IStudyLock studyLock,
    TimeProvider timeProvider) : ICommandHandler<ForceReleaseStudyLockCommand, Result>
{
    public async Task<Result> Handle(ForceReleaseStudyLockCommand request, CancellationToken cancellationToken)
    {
        var study = await studies.FindAsync(request.StudyId, cancellationToken);
        if (study is null)
        {
            return Result.Failure(StudyErrors.NotFound(request.StudyId));
        }

        study.NoteLockForceRelease(request.AdminId, request.Reason, timeProvider.GetUtcNow());
        await studies.SaveChangesAsync(cancellationToken);
        await studyLock.ForceRelease(request.StudyId);
        return Result.Success();
    }
}
