using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Locking;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public sealed record OpenStudyCommand(Guid StudyId, LockOwner Doctor) : ICommand<Result>;

internal sealed class OpenStudyCommandHandler(
    IStudyLock studyLock,
    IStudyRepository studies,
    TimeProvider timeProvider) : ICommandHandler<OpenStudyCommand, Result>
{
    public async Task<Result> Handle(OpenStudyCommand request, CancellationToken cancellationToken)
    {
        var (studyId, doctor) = request;

        var acquisition = await studyLock.TryAcquire(studyId, doctor);
        if (!acquisition.Acquired)
        {
            throw StudyLockConflict.HeldBy(studyId, acquisition.CurrentOwner);
        }

        var study = await studies.FindAsync(studyId, cancellationToken);
        if (study is null)
        {
            await studyLock.Release(studyId, doctor.DoctorId);
            return Result.Failure(StudyErrors.NotFound(studyId));
        }

        if (IsAlreadyReadBy(study, doctor.DoctorId))
        {
            return Result.Success();
        }

        try
        {
            study.StartRead(doctor.DoctorId, timeProvider.GetUtcNow());
            await studies.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await studyLock.Release(studyId, doctor.DoctorId);
            throw;
        }

        return Result.Success();
    }

    private static bool IsAlreadyReadBy(Study study, Guid doctorId) =>
        study.Status == StudyStatus.InProgress && study.DoctorId == doctorId;
}
